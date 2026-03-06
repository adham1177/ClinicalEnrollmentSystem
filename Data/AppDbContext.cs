using Microsoft.EntityFrameworkCore;
using ClinicalEnrollmentSystem.Models;
namespace ClinicalEnrollmentSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  public DbSet<Trial> Trials { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Index on Patient Name for faster lookups
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Name)
            .HasDatabaseName("IX_Patients_Name");


        // Index on Trial Status for faster filtering
        modelBuilder.Entity<Trial>()
            .HasIndex(t => t.Status)
            .HasDatabaseName("IX_Trials_Status");


        modelBuilder.Entity<Trial>().HasData(
            new Trial { Id = 1, Title = "Cardiovascular Risk Study", Status = "Active", MaxPatients = 3 },
            new Trial { Id = 2, Title = "Diabetes Prevention Trial", Status = "Active", MaxPatients = 2 }
        );


         modelBuilder.Entity<Patient>().HasData(
            new Patient { Id = 1, Name = "James Morrison", Age = 58, Condition = "Hypertension" },
            new Patient { Id = 2, Name = "Sarah Al-Hassan", Age = 64, Condition = "Type 2 Diabetes" },
            new Patient { Id = 3, Name = "Robert Chen", Age = 71, Condition = "Chronic Kidney Disease" }
        );
    }
}