using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicalEnrollmentSystem.Data;
using ClinicalEnrollmentSystem.Models;

namespace ClinicalEnrollmentSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PatientsController(AppDbContext context)
    {
        _context = context;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var patients = await _context.Patients
            .Include(p => p.Enrollments)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Age,
                p.Condition,
                ActiveTrials = p.Enrollments.Count(e => e.Status == "Active")
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Patient patient)
    {
        if (patient == null || string.IsNullOrEmpty(patient.Name) || patient.Age <= 0)
            return BadRequest("Invalid patient data.");

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = patient.Id }, patient);
    }
}