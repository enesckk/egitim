using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Services;

public class ExamSummaryQuery(
    IApplicationDbContext db,
    IAcademicCatalog academicCatalog) : IExamSummaryQuery
{
    public async Task<RecentExamSummaryDto?> GetRecentExamSummaryAsync(Guid studentId, Guid institutionId, CancellationToken ct = default)
    {
        var recentAttempts = await (from a in db.Set<StudentExamAttempt>().AsNoTracking()
                                    join e in db.Set<Exam>().AsNoTracking() on a.ExamId equals e.Id
                                    where a.StudentId == studentId && a.InstitutionId == institutionId && a.Status == AttemptStatus.Finalized
                                    orderby a.TakenAt descending, a.Id descending
                                    select new { a.ExamId, e.Title, e.ExamTypeId, a.TakenAt, a.TotalNet })
                                   .Take(2)
                                   .ToListAsync(ct);

        if (recentAttempts.Count == 0) return null;

        var first = recentAttempts[0];
        var secondNet = recentAttempts.Count > 1 ? recentAttempts[1].TotalNet : 0m;
        var delta = recentAttempts.Count > 1 ? Math.Round(first.TotalNet - secondNet, 2, MidpointRounding.AwayFromZero) : 0m;
        var et = await academicCatalog.GetExamTypeAsync(first.ExamTypeId, ct);

        return new RecentExamSummaryDto(
            first.ExamId,
            first.Title,
            et?.Name ?? "Deneme Sınavı",
            first.TakenAt,
            first.TotalNet,
            secondNet,
            delta
        );
    }
}
