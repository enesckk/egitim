using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public class GetStudentQuestionStyleAnalyticsHandler(
    IApplicationDbContext db,
    IExamResourceAccess resourceAccess)
{
    public async Task<List<StudentQuestionStyleAnalyticsDto>> HandleAsync(
        AcademicIntelligenceQuery query,
        CancellationToken ct = default)
    {
        await resourceAccess.ValidateStudentReadAccessAsync(query.StudentId, ct);

        // Fetch active controlled question styles
        var activeStyles = await db.Set<QuestionStyle>()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Select(s => new QuestionStyleMeta(s.Id, s.Code, s.Name, s.DisplayOrder))
            .ToListAsync(ct);

        var attemptsQuery = db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .Where(a => a.StudentId == query.StudentId && a.Status == AttemptStatus.Finalized && !a.IsDeleted);

        if (query.ExamTypeId.HasValue)
        {
            var examIdsForType = await db.Set<Exam>()
                .AsNoTracking()
                .Where(e => e.ExamTypeId == query.ExamTypeId.Value && !e.IsDeleted)
                .Select(e => e.Id)
                .ToListAsync(ct);

            attemptsQuery = attemptsQuery.Where(a => examIdsForType.Contains(a.ExamId));
        }

        if (query.From.HasValue)
        {
            attemptsQuery = attemptsQuery.Where(a => a.TakenAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            attemptsQuery = attemptsQuery.Where(a => a.TakenAt <= query.To.Value);
        }

        if (query.Window.HasValue && query.Window.Value > 0)
        {
            var topAttemptIds = await attemptsQuery
                .OrderByDescending(a => a.TakenAt)
                .Take(query.Window.Value)
                .Select(a => a.Id)
                .ToListAsync(ct);

            attemptsQuery = db.Set<StudentExamAttempt>()
                .AsNoTracking()
                .Where(a => topAttemptIds.Contains(a.Id));
        }

        var attempts = await attemptsQuery
            .OrderBy(a => a.TakenAt)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);

        if (attempts.Count == 0)
        {
            return QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, []);
        }

        var attemptIds = attempts.Select(a => a.Id).ToList();

        // Load raw question results for these finalized attempts
        var rawResults = await db.Set<StudentExamQuestionResult>()
            .AsNoTracking()
            .Where(qr => attemptIds.Contains(qr.StudentExamAttemptId) && !qr.IsDeleted)
            .Select(qr => new
            {
                qr.StudentExamAttemptId,
                qr.QuestionId,
                qr.IsCorrect,
                qr.IsBlank,
                qr.NetContribution
            })
            .ToListAsync(ct);

        if (rawResults.Count == 0)
        {
            return QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, []);
        }

        // Distinct question IDs to find style assignments
        var questionIds = rawResults.Select(r => r.QuestionId).Distinct().ToList();

        var styleAssignments = await db.Set<QuestionStyleAssignment>()
            .AsNoTracking()
            .Where(sa => questionIds.Contains(sa.QuestionId))
            .Select(sa => new { sa.QuestionId, sa.QuestionStyleId })
            .ToListAsync(ct);

        var styleLookup = styleAssignments
            .GroupBy(sa => sa.QuestionId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)g.Select(sa => sa.QuestionStyleId).ToList());

        var examIds = attempts.Select(a => a.ExamId).Distinct().ToList();
        var exams = await db.Set<Exam>()
            .AsNoTracking()
            .Where(e => examIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Title, ct);

        var attemptMap = attempts.ToDictionary(a => a.Id);

        var observations = new List<RawQuestionAttemptObservation>();
        foreach (var r in rawResults)
        {
            if (!styleLookup.TryGetValue(r.QuestionId, out var assignedStyles) || assignedStyles.Count == 0)
            {
                // Question has zero styles -> ignore for style analytics
                continue;
            }

            var attempt = attemptMap[r.StudentExamAttemptId];
            var examTitle = exams.TryGetValue(attempt.ExamId, out var title) ? title : "Exam";

            observations.Add(new RawQuestionAttemptObservation(
                attempt.Id,
                attempt.ExamId,
                examTitle,
                attempt.TakenAt,
                attempt.AttemptNumber,
                r.QuestionId,
                assignedStyles,
                r.IsCorrect,
                r.IsBlank,
                r.NetContribution
            ));
        }

        return QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);
    }
}
