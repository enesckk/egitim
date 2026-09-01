namespace EgitimPlatform.Modules.Students.Features;

public sealed record StudentDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Status,
    Guid InstitutionId,
    DateTimeOffset CreatedAt);
