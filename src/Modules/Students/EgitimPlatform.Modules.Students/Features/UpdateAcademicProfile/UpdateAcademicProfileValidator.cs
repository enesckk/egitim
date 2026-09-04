using FluentValidation;

namespace EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;

public class UpdateAcademicProfileValidator : AbstractValidator<UpdateAcademicProfileCommand>
{
    public UpdateAcademicProfileValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();

        RuleFor(x => x.SchoolName)
            .MaximumLength(200).When(x => x.SchoolName is not null);

        RuleFor(x => x.StudentNumber)
            .MaximumLength(50).When(x => x.StudentNumber is not null);

        RuleFor(x => x.GradeLevel)
            .InclusiveBetween(1, 12).When(x => x.GradeLevel.HasValue)
            .WithMessage("Grade level must be between 1 and 12.");
    }
}
