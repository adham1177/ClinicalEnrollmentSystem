using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicalEnrollmentSystem.Data;

namespace ClinicalEnrollmentSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrialManagementController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrialManagementController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> CloseTrial(int id)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // pessimistic concurrency control: lock the row so no one else can modify it until we're done

                var trial = await _context.Trials
                 .FromSqlRaw("SELECT * FROM Trials WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", id)
                 .Include(t => t.Enrollments)
                 .FirstOrDefaultAsync();

                if (trial == null)
                    return NotFound($"Trial with ID {id} not found.");

                if (trial.Status == "Closed")
                    return BadRequest("Trial is already closed.");

                var activeEnrollments = trial.Enrollments.Count(e => e.Status == "Active");

                trial.Status = "Closed";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = $"Trial '{trial.Title}' has been closed.",
                    ActiveEnrollments = activeEnrollments,
                    ClosedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"An error occurred while closing the trial: {ex.Message}");
            }
        });
    }
}