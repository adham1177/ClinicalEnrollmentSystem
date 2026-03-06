using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicalEnrollmentSystem.Data;
using ClinicalEnrollmentSystem.Models;

namespace ClinicalEnrollmentSystem.Controllers;

[ApiController]
[Route("api/[controller]")]

public class TrialsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrialsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/trials
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var trials = await _context.Trials
            .Include(t => t.Enrollments)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Status,
                t.MaxPatients,
                ActiveEnrollments = t.Enrollments.Count(e => e.Status == "Active"),
                AvailableSlots = t.MaxPatients - t.Enrollments.Count(e => e.Status == "Active")
            })
            .ToListAsync();

        return Ok(trials);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Trial trial)
    {
        if (trial == null || string.IsNullOrEmpty(trial.Title) || trial.MaxPatients <= 0)
            return BadRequest("Invalid trial data.");

        _context.Trials.Add(trial);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = trial.Id }, trial);
    }


    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _context.Trials
            .Include(t => t.Enrollments)
            .ThenInclude(e => e.Patient)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Status,
                t.MaxPatients,
                ActiveEnrollments = t.Enrollments.Count(e => e.Status == "Active"),
                WithdrawnEnrollments = t.Enrollments.Count(e => e.Status == "Withdrawn"),
                AvailableSlots = t.MaxPatients - t.Enrollments.Count(e => e.Status == "Active"),
                AveragePatientAge = t.Enrollments
                .Where(e => e.Status == "Active")
                .Average(e => e.Patient.Age),
                IsFull = t.Enrollments.Count(e => e.Status == "Active") >= t.MaxPatients,
            })
            .ToListAsync();

        return Ok(stats);
    }
}