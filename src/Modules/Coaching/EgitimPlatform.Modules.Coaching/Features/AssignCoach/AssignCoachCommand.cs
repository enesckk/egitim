namespace EgitimPlatform.Modules.Coaching.Features.AssignCoach;

public sealed record AssignCoachCommand(
    Guid StudentId,
    Guid CoachId,
    bool IsPrimary = false);
