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
    DateTimeOffset? EnrollmentDate = null,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    Guid? UserId = null);
