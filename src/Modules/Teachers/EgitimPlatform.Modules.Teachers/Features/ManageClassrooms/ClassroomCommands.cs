using FluentValidation;

namespace EgitimPlatform.Modules.Teachers.Features.ManageClassrooms;

public sealed record CreateClassroomCommand(
    Guid? InstitutionId,
    string Name,
    string? Branch = null,
    int? GradeLevel = null,
    string? AcademicYear = null);

public sealed record AssignClassroomTeacherCommand(
    Guid ClassroomId,
    Guid TeacherId,
    bool IsPrimary = false);

public sealed record AssignClassroomStudentCommand(
    Guid ClassroomId,
    Guid StudentId);

public sealed record ListClassroomsQuery(
    int? GradeLevel = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50);

public sealed record ClassroomTeacherDto(
    Guid TeacherId,
    string FullName,
    string? Title,
    bool IsPrimary);

public sealed record ClassroomStudentItemDto(
    Guid StudentId,
    string FullName,
    string? StudentNumber,
    int? GradeLevel);

public sealed record ClassroomDto(
    Guid Id,
    Guid InstitutionId,
    string Name,
    string? Branch,
    int? GradeLevel,
    string? AcademicYear,
    int StudentCount,
    int TeacherCount,
    IReadOnlyList<ClassroomTeacherDto> Teachers);

public sealed record ClassroomDetailDto(
    Guid Id,
    Guid InstitutionId,
    string Name,
    string? Branch,
    int? GradeLevel,
    string? AcademicYear,
    IReadOnlyList<ClassroomTeacherDto> Teachers,
    IReadOnlyList<ClassroomStudentItemDto> Students);

public class CreateClassroomValidator : AbstractValidator<CreateClassroomCommand>
{
    public CreateClassroomValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Branch).MaximumLength(50);
        RuleFor(x => x.GradeLevel).InclusiveBetween(1, 13).When(x => x.GradeLevel.HasValue);
        RuleFor(x => x.AcademicYear).MaximumLength(20);
    }
}

public class AssignClassroomTeacherValidator : AbstractValidator<AssignClassroomTeacherCommand>
{
    public AssignClassroomTeacherValidator()
    {
        RuleFor(x => x.ClassroomId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}

public class AssignClassroomStudentValidator : AbstractValidator<AssignClassroomStudentCommand>
{
    public AssignClassroomStudentValidator()
    {
        RuleFor(x => x.ClassroomId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
    }
}

public class ListClassroomsQueryValidator : AbstractValidator<ListClassroomsQuery>
{
    public ListClassroomsQueryValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
