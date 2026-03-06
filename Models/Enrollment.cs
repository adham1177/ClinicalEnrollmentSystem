namespace ClinicalEnrollmentSystem.Models;

public class Enrollment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int TrialId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";
    public Patient Patient { get; set; } = null!;
    public Trial Trial { get; set; } = null!;
}