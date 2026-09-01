using EgitimPlatform.Modules.Students.Entities;

namespace EgitimPlatform.Modules.Students.Features;

internal static class StudentExtensions
{
    public static StudentDto ToDto(this Student student)
    {
        return new StudentDto(
            student.Id,
            student.FirstName,
            student.LastName,
            $"{student.FirstName} {student.LastName}",
            student.Status.ToString(),
            student.InstitutionId,
            student.CreatedAt);
    }
}
