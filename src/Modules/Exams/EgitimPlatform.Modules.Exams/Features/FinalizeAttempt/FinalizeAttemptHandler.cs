using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.FinalizeAttempt;

public class FinalizeAttemptHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IExamResourceAccess resourceAccess,
    IExamScoringService scoringService,
    IRawExamAttemptFinalizer rawAttemptFinalizer,
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

        var examQuestions = await db.Set<ExamQuestion>()
            .AsNoTracking()
            .Where(eq => eq.ExamId == attempt.ExamId)
            .ToListAsync(ct);

        var rawAnswers = await db.Set<StudentExamQuestionResult>()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToListAsync(ct);

        var isRawMode = examQuestions.Count > 0 || rawAnswers.Count > 0;

        if (isRawMode)
        {
            // MODE B — Raw Question Results Atomic Scoring Pipeline
            await using var transaction = await db.BeginTransactionAsync(ct);

            var existingSubjectResults = await db.Set<StudentExamSubjectResult>()
                .Where(r => r.StudentExamAttemptId == attemptId)
                .ToListAsync(ct);
            db.Set<StudentExamSubjectResult>().RemoveRange(existingSubjectResults);

            var existingTopicResults = await db.Set<StudentExamTopicResult>()
                .Where(r => r.StudentExamAttemptId == attemptId)
                .ToListAsync(ct);
            db.Set<StudentExamTopicResult>().RemoveRange(existingTopicResults);

            var questionIds = examQuestions.Select(eq => eq.QuestionId).Distinct().ToList();
            var questionTopics = await db.Set<Question>()
                .AsNoTracking()
                .Where(q => questionIds.Contains(q.Id))
                .ToDictionaryAsync(q => q.Id, q => (Guid?)q.TopicId, ct);

            var existingRawGuids = rawAnswers.Select(r => r.ExamQuestionId).ToHashSet();

            rawAttemptFinalizer.FinalizeRawAttempt(
                attempt,
                exam,
                sections,
                examQuestions,
                rawAnswers,
                questionTopics,
                scoringService,
                DateTimeOffset.UtcNow);

            var newRawAnswers = attempt.QuestionResults.Where(r => !existingRawGuids.Contains(r.ExamQuestionId)).ToList();
            if (newRawAnswers.Count > 0)
            {
                db.Set<StudentExamQuestionResult>().AddRange(newRawAnswers);
            }

            db.Set<StudentExamSubjectResult>().AddRange(attempt.SubjectResults);
            db.Set<StudentExamTopicResult>().AddRange(attempt.TopicResults);

            if (currentUser.UserId.HasValue)
            {
                await auditService.LogAsync(currentUser.UserId.Value, "ExamAttemptFinalized", "StudentExamAttempt", attempt.Id.ToString(), attempt.InstitutionId, cancellationToken: ct);
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        else
        {
            // MODE A — Aggregate Only (Sprint 3 Backward Compatibility)
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

            attempt.IsRawAvailable = false;
            attempt.Status = AttemptStatus.Finalized;
            attempt.FinalizedAt = DateTimeOffset.UtcNow;

            if (currentUser.UserId.HasValue)
            {
                await auditService.LogAsync(currentUser.UserId.Value, "ExamAttemptFinalized", "StudentExamAttempt", attempt.Id.ToString(), attempt.InstitutionId, cancellationToken: ct);
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
