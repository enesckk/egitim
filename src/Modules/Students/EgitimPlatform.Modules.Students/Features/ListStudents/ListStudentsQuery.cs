namespace EgitimPlatform.Modules.Students.Features.ListStudents;

public sealed record ListStudentsQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null);
