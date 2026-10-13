namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface IQuestionUsageChecker
{
    Task<bool> IsQuestionLinkedToExamAsync(Guid questionId, CancellationToken ct = default);
}
