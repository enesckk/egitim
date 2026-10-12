namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface IQuestionUsageChecker
{
    Task<bool> IsQuestionUsedInAnyExamAsync(Guid questionId, CancellationToken cancellationToken = default);
}
