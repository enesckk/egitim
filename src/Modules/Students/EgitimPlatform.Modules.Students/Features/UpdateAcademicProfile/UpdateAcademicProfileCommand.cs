namespace EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;

public sealed record UpdateAcademicProfileCommand(
    Guid StudentId,
    string? SchoolName,
    string? StudentNumber,
    int? GradeLevel,
    DateTimeOffset? EnrollmentDate);
