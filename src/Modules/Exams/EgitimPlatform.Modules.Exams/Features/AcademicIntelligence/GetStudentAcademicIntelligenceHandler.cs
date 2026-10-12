using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public class GetStudentAcademicIntelligenceHandler(
    IApplicationDbContext db,
    IAcademicCatalog academicCatalog,
    IExamResourceAccess resourceAccess)
{
    public async Task<List<StudentSubjectPerformanceDto>> GetSubjectPerformanceAsync(
        AcademicIntelligenceQuery query,
        CancellationToken ct = default)
    {
        await resourceAccess.ValidateStudentReadAccessAsync(query.StudentId, ct);

        var attemptsQuery = db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .Where(a => a.StudentId == query.StudentId && a.Status == AttemptStatus.Finalized);

        if (query.ExamTypeId.HasValue)
        {
            var examIdsForType = await db.Set<Exam>()
                .AsNoTracking()
                .Where(e => e.ExamTypeId == query.ExamTypeId.Value)
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

        var attempts = await attemptsQuery.ToListAsync(ct);
        if (attempts.Count == 0) return [];

        var attemptIds = attempts.Select(a => a.Id).ToList();

        var subjectResults = await db.Set<StudentExamSubjectResult>()
            .AsNoTracking()
            .Where(sr => attemptIds.Contains(sr.StudentExamAttemptId))
            .ToListAsync(ct);

        if (subjectResults.Count == 0) return [];

        var sectionIds = subjectResults.Select(sr => sr.ExamSectionId).Distinct().ToList();
        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(sec => sectionIds.Contains(sec.Id))
            .ToDictionaryAsync(sec => sec.Id, ct);

        var subjectIds = sections.Values.Select(s => s.SubjectId).Distinct().ToList();
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        var attemptDict = attempts.ToDictionary(a => a.Id);

        var grouped = subjectResults
            .Select(sr =>
            {
                var sec = sections.GetValueOrDefault(sr.ExamSectionId);
                var att = attemptDict.GetValueOrDefault(sr.StudentExamAttemptId);
                return new
                {
                    SubjectId = sec?.SubjectId ?? Guid.Empty,
                    Attempt = att,
                    Result = sr
                };
            })
            .Where(x => x.SubjectId != Guid.Empty && x.Attempt != null)
            .GroupBy(x => x.SubjectId);

        var response = new List<StudentSubjectPerformanceDto>();

        foreach (var group in grouped)
        {
            var subjectId = group.Key;
            var subjectName = subjectNames.GetValueOrDefault(subjectId, "Bilinmeyen Ders");

            var examCount = group.Select(x => x.Attempt!.ExamId).Distinct().Count();
            var attemptCount = group.Select(x => x.Attempt!.Id).Distinct().Count();

            var totalCorrect = group.Sum(x => x.Result.Correct);
            var totalWrong = group.Sum(x => x.Result.Wrong);
            var totalBlank = group.Sum(x => x.Result.Blank);
            var totalNet = group.Sum(x => x.Result.Net);

            var totalQuestions = totalCorrect + totalWrong + totalBlank;
            var attemptedCount = totalCorrect + totalWrong;

            var accuracy = attemptedCount > 0 ? Math.Round((decimal)totalCorrect / attemptedCount, 4) : 0m;
            var successRate = totalQuestions > 0 ? Math.Round((decimal)totalCorrect / totalQuestions, 4) : 0m;
            var coverage = totalQuestions > 0 ? Math.Round((decimal)attemptedCount / totalQuestions, 4) : 0m;

            var rawAttemptCount = group.Count(x => x.Attempt!.IsRawAvailable);
            var aggregateAttemptCount = group.Count(x => !x.Attempt!.IsRawAvailable);

            response.Add(new StudentSubjectPerformanceDto(
                subjectId,
                subjectName,
                examCount,
                attemptCount,
                totalQuestions,
                attemptedCount,
                totalCorrect,
                totalWrong,
                totalBlank,
                totalNet,
                accuracy,
                successRate,
                coverage,
                rawAttemptCount,
                aggregateAttemptCount
            ));
        }

        return response.OrderBy(s => s.SubjectName).ToList();
    }

    public async Task<List<StudentTopicPerformanceDto>> GetTopicPerformanceAsync(
        AcademicIntelligenceQuery query,
        CancellationToken ct = default)
    {
        await resourceAccess.ValidateStudentReadAccessAsync(query.StudentId, ct);

        // RAW attempts ONLY (IsRawAvailable == true) — NEVER fabricate topic totals from aggregate attempts
        var attemptsQuery = db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .Where(a => a.StudentId == query.StudentId && a.Status == AttemptStatus.Finalized && a.IsRawAvailable);

        if (query.ExamTypeId.HasValue)
        {
            var examIdsForType = await db.Set<Exam>()
                .AsNoTracking()
                .Where(e => e.ExamTypeId == query.ExamTypeId.Value)
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

        var attempts = await attemptsQuery.ToListAsync(ct);
        if (attempts.Count == 0) return [];

        var attemptIds = attempts.Select(a => a.Id).ToList();

        var topicResults = await db.Set<StudentExamTopicResult>()
            .AsNoTracking()
            .Where(tr => attemptIds.Contains(tr.StudentExamAttemptId))
            .ToListAsync(ct);

        if (topicResults.Count == 0) return [];

        var topicIds = topicResults.Select(tr => tr.TopicId).Distinct().ToList();
        var topicNames = await academicCatalog.GetTopicNamesAsync(topicIds, ct);

        var sectionIds = topicResults.Select(tr => tr.ExamSectionId).Distinct().ToList();
        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(sec => sectionIds.Contains(sec.Id))
            .ToDictionaryAsync(sec => sec.Id, ct);

        var subjectIds = sections.Values.Select(s => s.SubjectId).Distinct().ToList();
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        var attemptDict = attempts.ToDictionary(a => a.Id);

        var grouped = topicResults
            .Select(tr =>
            {
                var sec = sections.GetValueOrDefault(tr.ExamSectionId);
                var att = attemptDict.GetValueOrDefault(tr.StudentExamAttemptId);
                return new
                {
                    TopicId = tr.TopicId,
                    SubjectId = sec?.SubjectId ?? Guid.Empty,
                    Attempt = att,
                    Result = tr
                };
            })
            .Where(x => x.Attempt != null)
            .GroupBy(x => x.TopicId);

        var response = new List<StudentTopicPerformanceDto>();

        foreach (var group in grouped)
        {
            var topicId = group.Key;
            var topicName = topicNames.GetValueOrDefault(topicId, "Bilinmeyen Konu");
            var subjectId = group.First().SubjectId;
            var subjectName = subjectNames.GetValueOrDefault(subjectId, "Bilinmeyen Ders");

            var examCount = group.Select(x => x.Attempt!.ExamId).Distinct().Count();
            var attemptCount = group.Select(x => x.Attempt!.Id).Distinct().Count();

            var totalCorrect = group.Sum(x => x.Result.Correct);
            var totalWrong = group.Sum(x => x.Result.Wrong);
            var totalBlank = group.Sum(x => x.Result.Blank);
            var totalNet = group.Sum(x => x.Result.Net);

            var totalQuestions = totalCorrect + totalWrong + totalBlank;
            var attemptedCount = totalCorrect + totalWrong;

            var accuracy = attemptedCount > 0 ? Math.Round((decimal)totalCorrect / attemptedCount, 4) : 0m;
            var successRate = totalQuestions > 0 ? Math.Round((decimal)totalCorrect / totalQuestions, 4) : 0m;
            var coverage = totalQuestions > 0 ? Math.Round((decimal)attemptedCount / totalQuestions, 4) : 0m;

            response.Add(new StudentTopicPerformanceDto(
                topicId,
                topicName,
                subjectId,
                subjectName,
                examCount,
                attemptCount,
                totalQuestions,
                attemptedCount,
                totalCorrect,
                totalWrong,
                totalBlank,
                totalNet,
                accuracy,
                successRate,
                coverage
            ));
        }

        return response.OrderBy(t => t.SubjectName).ThenBy(t => t.TopicName).ToList();
    }

    public async Task<List<StudentLearningOutcomePerformanceDto>> GetLearningOutcomePerformanceAsync(
        AcademicIntelligenceQuery query,
        CancellationToken ct = default)
    {
        await resourceAccess.ValidateStudentReadAccessAsync(query.StudentId, ct);

        // RAW attempts ONLY (IsRawAvailable == true)
        var attemptsQuery = db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .Where(a => a.StudentId == query.StudentId && a.Status == AttemptStatus.Finalized && a.IsRawAvailable);

        if (query.ExamTypeId.HasValue)
        {
            var examIdsForType = await db.Set<Exam>()
                .AsNoTracking()
                .Where(e => e.ExamTypeId == query.ExamTypeId.Value)
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

        var attempts = await attemptsQuery.ToListAsync(ct);
        if (attempts.Count == 0) return [];

        var attemptIds = attempts.Select(a => a.Id).ToList();

        // Join StudentExamQuestionResult -> Question (where LearningOutcomeId != null)
        var rawQuestionResults = await db.Set<StudentExamQuestionResult>()
            .AsNoTracking()
            .Where(qr => attemptIds.Contains(qr.StudentExamAttemptId))
            .ToListAsync(ct);

        if (rawQuestionResults.Count == 0) return [];

        var questionIds = rawQuestionResults.Select(qr => qr.QuestionId).Distinct().ToList();
        var questions = await db.Set<Question>()
            .AsNoTracking()
            .Where(q => questionIds.Contains(q.Id) && q.LearningOutcomeId.HasValue)
            .ToDictionaryAsync(q => q.Id, ct);

        if (questions.Count == 0) return [];

        var loIds = questions.Values.Select(q => q.LearningOutcomeId!.Value).Distinct().ToList();
        var learningOutcomes = await db.Set<LearningOutcome>()
            .AsNoTracking()
            .Where(lo => loIds.Contains(lo.Id))
            .ToDictionaryAsync(lo => lo.Id, ct);

        var topicIds = questions.Values.Select(q => q.TopicId).Distinct().ToList();
        var topicNames = await academicCatalog.GetTopicNamesAsync(topicIds, ct);

        var subjectIds = questions.Values.Select(q => q.SubjectId).Distinct().ToList();
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        var subTopicIds = questions.Values.Where(q => q.SubTopicId.HasValue).Select(q => q.SubTopicId!.Value).Distinct().ToList();
        var subTopics = await db.Set<SubTopic>()
            .AsNoTracking()
            .Where(st => subTopicIds.Contains(st.Id))
            .ToDictionaryAsync(st => st.Id, ct);

        var attemptDict = attempts.ToDictionary(a => a.Id);

        var joinedResults = rawQuestionResults
            .Select(qr =>
            {
                var q = questions.GetValueOrDefault(qr.QuestionId);
                var att = attemptDict.GetValueOrDefault(qr.StudentExamAttemptId);
                return new
                {
                    QuestionResult = qr,
                    Question = q,
                    Attempt = att
                };
            })
            .Where(x => x.Question != null && x.Attempt != null)
            .GroupBy(x => x.Question!.LearningOutcomeId!.Value);

        var response = new List<StudentLearningOutcomePerformanceDto>();

        foreach (var group in joinedResults)
        {
            var loId = group.Key;
            var lo = learningOutcomes.GetValueOrDefault(loId);
            var sampleQuestion = group.First().Question!;

            var loCode = lo?.Code ?? "KAZ";
            var loTitle = lo?.Name ?? "Kazanım";

            var topicId = sampleQuestion.TopicId;
            var topicName = topicNames.GetValueOrDefault(topicId, "Konu");
            var subjectId = sampleQuestion.SubjectId;
            var subjectName = subjectNames.GetValueOrDefault(subjectId, "Ders");

            var subTopicId = sampleQuestion.SubTopicId;
            var subTopicName = subTopicId.HasValue ? subTopics.GetValueOrDefault(subTopicId.Value)?.Name : null;

            var examCount = group.Select(x => x.Attempt!.ExamId).Distinct().Count();
            var totalQuestions = group.Count();

            var totalCorrect = group.Count(x => x.QuestionResult.IsCorrect == true);
            var totalWrong = group.Count(x => x.QuestionResult.IsCorrect == false && !x.QuestionResult.IsBlank);
            var totalBlank = group.Count(x => x.QuestionResult.IsBlank);

            var attemptedCount = totalCorrect + totalWrong;

            var accuracy = attemptedCount > 0 ? Math.Round((decimal)totalCorrect / attemptedCount, 4) : 0m;
            var successRate = totalQuestions > 0 ? Math.Round((decimal)totalCorrect / totalQuestions, 4) : 0m;
            var coverage = totalQuestions > 0 ? Math.Round((decimal)attemptedCount / totalQuestions, 4) : 0m;

            response.Add(new StudentLearningOutcomePerformanceDto(
                loId,
                loCode,
                loTitle,
                subTopicId,
                subTopicName,
                topicId,
                topicName,
                subjectId,
                subjectName,
                examCount,
                totalQuestions,
                attemptedCount,
                totalCorrect,
                totalWrong,
                totalBlank,
                accuracy,
                successRate,
                coverage
            ));
        }

        return response.OrderBy(lo => lo.SubjectName).ThenBy(lo => lo.TopicName).ThenBy(lo => lo.LearningOutcomeCode).ToList();
    }

    public async Task<AcademicCoverageSummaryDto> GetAcademicCoverageSummaryAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        await resourceAccess.ValidateStudentReadAccessAsync(studentId, ct);

        var finalizedAttempts = await db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.Status == AttemptStatus.Finalized)
            .ToListAsync(ct);

        int totalAttempts = finalizedAttempts.Count;
        int rawAttemptsCount = finalizedAttempts.Count(a => a.IsRawAvailable);
        int aggregateAttemptsCount = finalizedAttempts.Count(a => !a.IsRawAvailable);

        decimal rawCoveragePercentage = totalAttempts > 0
            ? Math.Round((decimal)rawAttemptsCount / totalAttempts * 100m, 2)
            : 0m;

        var rawAttemptIds = finalizedAttempts.Where(a => a.IsRawAvailable).Select(a => a.Id).ToList();

        int totalQuestionsEvaluated = 0;
        int taggedQuestionsEvaluated = 0;

        if (rawAttemptIds.Count > 0)
        {
            var rawQuestionResults = await db.Set<StudentExamQuestionResult>()
                .AsNoTracking()
                .Where(qr => rawAttemptIds.Contains(qr.StudentExamAttemptId))
                .ToListAsync(ct);

            totalQuestionsEvaluated = rawQuestionResults.Count;

            var questionIds = rawQuestionResults.Select(qr => qr.QuestionId).Distinct().ToList();
            var taggedQuestionIds = await db.Set<Question>()
                .AsNoTracking()
                .Where(q => questionIds.Contains(q.Id) && q.LearningOutcomeId.HasValue)
                .Select(q => q.Id)
                .ToHashSetAsync(ct);

            taggedQuestionsEvaluated = rawQuestionResults.Count(qr => taggedQuestionIds.Contains(qr.QuestionId));
        }

        decimal tagCoveragePercentage = totalQuestionsEvaluated > 0
            ? Math.Round((decimal)taggedQuestionsEvaluated / totalQuestionsEvaluated * 100m, 2)
            : 0m;

        return new AcademicCoverageSummaryDto(
            studentId,
            totalAttempts,
            rawAttemptsCount,
            aggregateAttemptsCount,
            rawCoveragePercentage,
            totalQuestionsEvaluated,
            taggedQuestionsEvaluated,
            tagCoveragePercentage
        );
    }
}
