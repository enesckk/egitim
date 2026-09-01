using FluentValidation;

namespace EgitimPlatform.Modules.Coaching.Features.AssignCoach;

public class AssignCoachValidator : AbstractValidator<AssignCoachCommand>
{
    public AssignCoachValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty().WithMessage("Student ID is required.");
        RuleFor(x => x.CoachId).NotEmpty().WithMessage("Coach ID is required.");
        RuleFor(x => x.CoachId).NotEqual(x => x.StudentId).WithMessage("A student cannot be assigned to themselves as coach.");
    }
}
