using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.FinalizeAttempt;

public class FinalizeAttemptHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IExamResourceAccess resourceAccess,
    IExamScoringService scoringService,
    IAuditService auditService)
{
    public async Task HandleAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await resourceAccess.ValidateAttemptWriteAccessAsync(attemptId, ct);

        if (attempt.Status == AttemptStatus.Finalized)
            throw new ConflictException("Attempt is already finalized.");

        var exam = await db.Set<Exam>().AsNoTracking().SingleAsync(e => e.Id == attempt.ExamId, ct);

        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == attempt.ExamId)
            .ToDictionaryAsync(s => s.Id, ct);

        var subjectResults = await db.Set<StudentExamSubjectResult>()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToListAsync(ct);

        var topicResults = await db.Set<StudentExamTopicResult>()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToListAsync(ct);

        int totalCorrect = 0, totalWrong = 0, totalBlank = 0;
        decimal totalNet = 0m;

        foreach (var sr in subjectResults)
        {
            var secPenalty = sections.TryGetValue(sr.ExamSectionId, out var sec) && sec.WrongAnswerPenalty.HasValue
                ? sec.WrongAnswerPenalty.Value
                : exam.WrongAnswerPenalty;

            sr.Net = scoringService.CalculateNet(sr.Correct, sr.Wrong, secPenalty);

            totalCorrect += sr.Correct;
            totalWrong += sr.Wrong;
            totalBlank += sr.Blank;
            totalNet += sr.Net;
        }

        foreach (var tr in topicResults)
        {
            var secPenalty = sections.TryGetValue(tr.ExamSectionId, out var sec) && sec.WrongAnswerPenalty.HasValue
                ? sec.WrongAnswerPenalty.Value
                : exam.WrongAnswerPenalty;

            tr.Net = scoringService.CalculateNet(tr.Correct, tr.Wrong, secPenalty);
        }

        attempt.TotalCorrect = totalCorrect;
        attempt.TotalWrong = totalWrong;
        attempt.TotalBlank = totalBlank;
        attempt.TotalNet = totalNet;

        attempt.Status = AttemptStatus.Finalized;
        attempt.FinalizedAt = DateTimeOffset.UtcNow;

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "ExamAttemptFinalized", "StudentExamAttempt", attempt.Id.ToString(), attempt.InstitutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
