using FluentValidation;
namespace EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
public record CreateTeacherCommand(Guid InstitutionId, Guid UserId, string FirstName, string LastName, string? Title = null);
public record TeacherSubjectCommand(Guid TeacherId, Guid SubjectId);
public record TeacherDto(Guid Id, string FirstName, string LastName);
public record TeacherSubjectDto(Guid Id, Guid TeacherId, Guid SubjectId);
public class CreateTeacherValidator : AbstractValidator<CreateTeacherCommand>
{
    public CreateTeacherValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty(); RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100); RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).MaximumLength(50);
    }
}
public class TeacherSubjectValidator : AbstractValidator<TeacherSubjectCommand>
{
    public TeacherSubjectValidator() { RuleFor(x => x.TeacherId).NotEmpty(); RuleFor(x => x.SubjectId).NotEmpty(); }
}
