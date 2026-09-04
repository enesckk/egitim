using FluentValidation;

namespace EgitimPlatform.Modules.Students.Features.CreateStudentGoal;

public class CreateStudentGoalValidator : AbstractValidator<CreateStudentGoalCommand>
{
    public CreateStudentGoalValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Goal title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => x.Description is not null);

        RuleFor(x => x.TargetScore)
            .GreaterThanOrEqualTo(0).When(x => x.TargetScore.HasValue)
            .WithMessage("Target score must be non-negative.");

        RuleFor(x => x.TargetRank)
            .GreaterThan(0).When(x => x.TargetRank.HasValue)
            .WithMessage("Target rank must be positive.");

        RuleFor(x => x.TargetSchoolName)
            .MaximumLength(200).When(x => x.TargetSchoolName is not null);
    }
}
