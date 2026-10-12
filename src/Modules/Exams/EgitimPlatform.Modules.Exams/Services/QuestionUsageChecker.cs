using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Services;

public class QuestionUsageChecker(IApplicationDbContext db) : IQuestionUsageChecker
{
    public Task<bool> IsQuestionLinkedToExamAsync(Guid questionId, CancellationToken ct = default)
    {
        return db.Set<ExamQuestion>().AnyAsync(eq => eq.QuestionId == questionId && !eq.IsDeleted, ct);
    }
}
