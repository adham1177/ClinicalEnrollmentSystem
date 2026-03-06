using System.ComponentModel.DataAnnotations;

namespace ClinicalEnrollmentSystem.Models;

public class Trial
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxPatients { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}