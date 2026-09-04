using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Students.Entities;

public class Student : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public StudentStatus Status { get; set; } = StudentStatus.Active;

    // Sprint 2 — Academic profile fields (all nullable, not every student has these yet)
    /// <summary>School name — free text, not a FK to a School entity.</summary>
    public string? SchoolName { get; set; }

    /// <summary>Student number within the institution. Unique per institution when set.</summary>
    public string? StudentNumber { get; set; }

    /// <summary>Grade level (e.g. 9, 10, 11, 12). Nullable until set.</summary>
    public int? GradeLevel { get; set; }

    /// <summary>Date the student enrolled in the institution.</summary>
    public DateTimeOffset? EnrollmentDate { get; set; }
}
