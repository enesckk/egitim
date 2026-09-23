namespace EgitimPlatform.BuildingBlocks.Interfaces;

public record RecentExamSummaryDto(
    Guid LatestExamId,
    string Title,
    string ExamType,
    DateTimeOffset TakenAt,
    decimal TotalNet,
    decimal PreviousNet,
    decimal Delta
);

public interface IExamSummaryQuery
{
    Task<RecentExamSummaryDto?> GetRecentExamSummaryAsync(Guid studentId, Guid institutionId, CancellationToken ct = default);
}
