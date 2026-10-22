namespace EgitimPlatform.BuildingBlocks.Interfaces;

public record AcademicFactTopic(
    Guid TopicId,
    string TopicName,
    string ClassificationState,
    decimal NetRate,
    decimal SuccessRate
);

public record AcademicFactTrend(
    string SubjectName,
    string TrendDirection,
    string Message
);

public record AcademicFactRecommendation(
    string Title,
    string RecommendationType,
    int Priority
);

public record AcademicFactPlanSummary(
    string Status,
    int CompletedTaskCount,
    int TotalTaskCount,
    decimal UtilizationRate
);

public record AcademicFactQuestionStyle(
    Guid QuestionStyleId,
    string Code,
    string Name,
    string ClassificationState,
    decimal? SuccessRate,
    string? TrendDirection
);

public record AcademicFactPackage(
    Guid StudentId,
    string Audience,
    IReadOnlyList<AcademicFactTopic> Classifications,
    IReadOnlyList<AcademicFactTrend> Trends,
    IReadOnlyList<AcademicFactRecommendation> Recommendations,
    AcademicFactPlanSummary? PlanSummary,
    string FactFingerprint,
    IReadOnlyList<AcademicFactQuestionStyle>? QuestionStyles = null
);

public record AcademicAiSummaryDto(
    string Audience,
    string Summary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> ImprovementAreas,
    IReadOnlyList<string> TrendNotes,
    IReadOnlyList<string> SuggestedFocus,
    bool IsAvailable,
    DateTimeOffset GeneratedAt
);

public interface IAcademicInterpretationService
{
    Task<AcademicAiSummaryDto> GenerateInterpretationAsync(AcademicFactPackage factPackage, CancellationToken ct = default);
}
