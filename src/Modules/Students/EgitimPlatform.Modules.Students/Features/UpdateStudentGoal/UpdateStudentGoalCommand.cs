namespace EgitimPlatform.Modules.Students.Features.UpdateStudentGoal;

public sealed record UpdateStudentGoalCommand(
    Guid GoalId,
    string? Title,
    string? Description,
    Guid? TargetExamTypeId,
    int? TargetScore,
    int? TargetRank,
    string? TargetSchoolName,
    DateTimeOffset? EffectiveDate);
