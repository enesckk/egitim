namespace EgitimPlatform.Modules.Students.Features;

public sealed record StudentGoalDto(
    Guid Id,
    Guid StudentId,
    string Title,
    string? Description,
    Guid? TargetExamTypeId,
    int? TargetScore,
    int? TargetRank,
    string? TargetSchoolName,
    DateTimeOffset EffectiveDate,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record StudentGoalHistoryDto(
    Guid Id,
    Guid StudentGoalId,
    string Action,
    string? PreviousValuesJson,
    string? NewValuesJson,
    DateTimeOffset ChangedAt,
    Guid? ChangedBy);
