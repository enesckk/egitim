using FluentValidation;

namespace EgitimPlatform.Modules.Students.Features.CreateStudent;

public class CreateStudentValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).When(x => x.UserId.HasValue);

        RuleFor(x => x.SchoolName)
            .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.SchoolName));

        RuleFor(x => x.StudentNumber)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.StudentNumber));

        RuleFor(x => x.GradeLevel)
            .InclusiveBetween(1, 13).When(x => x.GradeLevel.HasValue);
    }
}
