namespace EgitimPlatform.Modules.Students.Features.CreateStudent;

public sealed record CreateStudentCommand(
    string FirstName,
    string LastName,
    string? Email);
