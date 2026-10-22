namespace EgitimPlatform.Modules.Students.Features.CreateStudent;

public sealed record CreateStudentCommand(
    string FirstName,
    string LastName,
    string? Email,
    Guid? UserId = null,
    string? SchoolName = null,
    string? StudentNumber = null,
    int? GradeLevel = null,
    string? Password = null);
