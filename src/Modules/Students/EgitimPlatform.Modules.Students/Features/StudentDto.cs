namespace EgitimPlatform.Modules.Students.Features;

public sealed record StudentDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Status,
    Guid InstitutionId,
    DateTimeOffset CreatedAt,
    // Sprint 2 — Academic profile
    string? SchoolName = null,
    string? StudentNumber = null,
    int? GradeLevel = null,
    DateTimeOffset? EnrollmentDate = null);
