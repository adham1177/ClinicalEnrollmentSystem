# Clinical Enrollment System

A RESTful API built with ASP.NET Core and Entity Framework Core that demonstrates
advanced database concepts including transactions, concurrency control, aggregate queries,
indexes, and soft deletes — applied to a clinical trial patient enrollment scenario.

## 🚀 Live Demo
**API is live at:** https://clinical-enrollment-system.azurewebsites.net/swagger

## Tech Stack

- **ASP.NET Core 10** — Web API framework
- **Entity Framework Core** — ORM for database access
- **Azure SQL Database** — Fully managed cloud relational database
- **Swagger** — API documentation and testing UI
- **Docker** — Containerized deployment
- **Microsoft Azure** — Cloud hosting via App Service

## Database Concepts Demonstrated

### 1. Transactions & Rollbacks
The enrollment process uses an explicit database transaction wrapping multiple steps:
- Validate patient exists
- Validate trial exists and is active
- Check trial capacity
- Check patient not already enrolled
- Create enrollment record

If any step fails, the entire transaction is rolled back — no partial data is ever saved.

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // ... multiple operations ...
    await transaction.CommitAsync();
}
catch (Exception)
{
    await transaction.RollbackAsync();
}
```

### 2. Optimistic Concurrency
Prevents race conditions when two doctors try to enroll the last available slot simultaneously.
Uses a `RowVersion` timestamp column — EF Core throws `DbUpdateConcurrencyException` if the
row was modified between read and write.

```csharp
[Timestamp]
public byte[] RowVersion { get; set; } = null!;
```

If a conflict is detected, the API returns `409 Conflict` instead of silently over-enrolling the trial.

### 3. Pessimistic Concurrency
Used when closing a trial — a critical one-time operation that must not run concurrently.
Locks the specific row using SQL Server hints `UPDLOCK` and `ROWLOCK` for the duration of the transaction.

```sql
SELECT * FROM Trials WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}
```

| | Optimistic | Pessimistic |
|---|---|---|
| Strategy | Detect conflicts after | Prevent conflicts before |
| Locking | No row lock | Row locked during transaction |
| Best for | Frequent operations | Critical one-time operations |
| Our usage | Enrollment | Closing a trial |

### 4. Indexes
Indexes are added on frequently queried columns to improve read performance.

```csharp
modelBuilder.Entity<Patient>()
    .HasIndex(p => p.Name)
    .HasDatabaseName("IX_Patients_Name");

modelBuilder.Entity<Trial>()
    .HasIndex(t => t.Status)
    .HasDatabaseName("IX_Trials_Status");
```

### 5. Aggregate Queries
The `/api/trials/stats` endpoint demonstrates database-level aggregations — all computed
in SQL, not in memory.

```csharp
ActiveEnrollments = t.Enrollments.Count(e => e.Status == "Active"),
AveragePatientAge = t.Enrollments
    .Where(e => e.Status == "Active")
    .Select(e => e.Patient.Age)
    .Average(),
IsFull = t.Enrollments.Count(e => e.Status == "Active") >= t.MaxPatients
```

### 6. Soft Delete
Withdrawing a patient does not delete the enrollment record — it marks it as `Withdrawn`.
This preserves audit history and allows reporting on withdrawn patients.

```csharp
enrollment.Status = "Withdrawn"; // Never DELETE from the database
await _context.SaveChangesAsync();
```

### 7. Many-to-Many Relationship
A patient can enroll in multiple trials. A trial can have multiple patients.
Modeled through an `Enrollment` junction table that also carries extra data
(enrollment date, status).

```
Patient ──── Enrollment ──── Trial
              EnrolledAt
              Status
```

### 8. Seed Data
Demo data is automatically inserted when the app starts via EF Core seed data,
ensuring the API always has realistic data to demonstrate.

## Endpoints

### Enrollment
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/enrollment | Get all enrollments |
| POST | /api/enrollment | Enroll a patient in a trial (with transaction) |
| DELETE | /api/enrollment/{id} | Withdraw a patient (soft delete) |

### Trials
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/trials | Get all trials with enrollment counts |
| GET | /api/trials/stats | Get aggregate statistics per trial |
| POST | /api/trials | Create a new trial |

### Trial Management
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/trialmanagement/{id}/close | Close a trial (pessimistic lock) |

### Patients
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/patients | Get all patients with active trial count |
| POST | /api/patients | Create a new patient |

## Getting Started

### Option 1: Live Demo
Visit the live API directly:
```
https://clinical-enrollment-system.azurewebsites.net/swagger
```

### Option 2: Run with Docker
```bash
git clone https://github.com/adham1177/ClinicalEnrollmentSystem.git
cd ClinicalEnrollmentSystem
docker build -t clinical-enrollment-system .
docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="your-connection-string" clinical-enrollment-system
```
Then open: `http://localhost:8080/swagger`

### Option 3: Run Locally
```bash
git clone https://github.com/adham1177/ClinicalEnrollmentSystem.git
cd ClinicalEnrollmentSystem
cp appsettings.example.json appsettings.json
```

Edit `appsettings.json` and add your connection string, then:
```bash
dotnet restore
dotnet ef database update
dotnet run
```

## Data Models

### Trial
- `Id` — unique identifier
- `Title` — trial name
- `Status` — Active / Closed
- `MaxPatients` — maximum enrollment capacity
- `RowVersion` — optimistic concurrency token
- `Enrollments` — list of enrollments

### Patient
- `Id` — unique identifier
- `Name` — patient name
- `Age` — patient age
- `Condition` — medical condition
- `Enrollments` — list of enrollments

### Enrollment
- `Id` — unique identifier
- `PatientId` — foreign key to patient
- `TrialId` — foreign key to trial
- `EnrolledAt` — enrollment timestamp
- `Status` — Active / Withdrawn