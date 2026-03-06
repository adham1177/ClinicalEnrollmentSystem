using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicalEnrollmentSystem.Data;
using ClinicalEnrollmentSystem.Models;

namespace ClinicalEnrollmentSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentController : ControllerBase
{
    private readonly AppDbContext _context;

    public EnrollmentController(AppDbContext context)
    {
        _context = context;
    }

     // GET: api/enrollment
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await _context.Enrollments
            .Include(e => e.Patient)
            .Include(e => e.Trial)
            .Select(e => new
            {
                e.Id,
                Patient = e.Patient.Name,
                Trial = e.Trial.Title,
                e.EnrolledAt,
                e.Status
            })
            .ToListAsync();

        return Ok(enrollments);
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(int patientId, int trialId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
        // ── START TRANSACTION ──────────────────────────────────
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Validate patient and trial existence
            var patient = await _context.Patients.FindAsync(patientId);

            if (patient == null)
                return NotFound($"Patient with ID {patientId} not found.");

            // Step 2: Validate trial existence and status
            var trial = await _context.Trials
                .Include(t => t.Enrollments)
                .FirstOrDefaultAsync(t => t.Id == trialId);


            if (trial == null)
                return NotFound($"Trial with ID {trialId} not found.");

            if (trial.Status != "Active")
                return BadRequest($"Cannot enroll in a trial {trialId} that is not active.");

            // step 3: Check if trial has capacity
            var currentEnrollmentCount = trial.Enrollments.Count(e => e.Status == "Active");

            if (currentEnrollmentCount >= trial.MaxPatients)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Trial {trialId} has reached its maximum participant limit.");
            }

            // Step 4: Check if patient is already enrolled in the trial
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.PatientId == patientId && e.TrialId == trialId && e.Status == "Active");

            if (alreadyEnrolled)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Patient {patientId} is already enrolled in trial {trialId}.");
            }

            // Step 5: Create enrollment record

            var enrollment = new Enrollment
            {
                PatientId = patientId,
                TrialId = trialId,
                EnrolledAt = DateTime.UtcNow,
                Status = "Active"
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();


            return Ok(new
            {
                Message = $"Patient {patientId} successfully enrolled in trial {trialId}.",
                EnrollmentId = enrollment.Id,
                enrollment.EnrolledAt
            });

        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict("Enrollment failed due to a concurrency conflict. Please try again.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"Enrollment failed and was rolled back: {ex.Message}");
        }
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Withdraw(int id)
    {
        try
        {
            var enrollment = await _context.Enrollments
            .Include(e => e.Patient)
            .Include(e => e.Trial)
            .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
                return NotFound($"Enrollment with ID {id} not found.");


            enrollment.Status = "Withdrawn";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Patient '{enrollment.Patient.Name}' withdrawn from '{enrollment.Trial.Title}'",
                WithdrawnAt = DateTime.UtcNow
            });


        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Withdrawal failed: {ex.Message}");
        }
    }


}