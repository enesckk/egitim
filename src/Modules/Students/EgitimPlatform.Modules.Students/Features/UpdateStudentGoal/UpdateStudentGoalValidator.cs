using FluentValidation;

namespace EgitimPlatform.Modules.Students.Features.UpdateStudentGoal;

public class UpdateStudentGoalValidator : AbstractValidator<UpdateStudentGoalCommand>
{
    public UpdateStudentGoalValidator()
    {
        RuleFor(x => x.GoalId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.TargetScore).GreaterThanOrEqualTo(0).When(x => x.TargetScore.HasValue);
        RuleFor(x => x.TargetRank).GreaterThan(0).When(x => x.TargetRank.HasValue);
        RuleFor(x => x.TargetSchoolName).MaximumLength(200).When(x => x.TargetSchoolName is not null);
    }
}
