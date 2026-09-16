namespace EgitimPlatform.Modules.Students.Features.CreateStudentGoal;

public sealed record CreateStudentGoalCommand(
    Guid StudentId,
    string Title,
    string? Description,
    Guid? TargetExamTypeId,
    int? TargetScore,
    int? TargetRank,
    string? TargetSchoolName,
    DateTimeOffset? EffectiveDate);
