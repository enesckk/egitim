using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Questions.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public class GetStudentAcademicTrendHandler(
    IApplicationDbContext db,
    IAcademicCatalog academicCatalog,
    IExamResourceAccess resourceAccess)
{
    private static void ValidateQueryParameters(AcademicTrendQuery query)
    {
        if (query.WindowSize < 2 || query.WindowSize > 10)
            throw new ValidationException("WindowSize must be between 2 and 10.");

        if (query.Limit.HasValue && (query.Limit.Value < 1 || query.Limit.Value > 100))
            throw new ValidationException("Limit must be between 1 and 100.");

        if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value)
            throw new ValidationException("From date cannot be after To date.");
    }

    public async Task<List<SubjectTrendDto>> GetSubjectTrendsAsync(
        AcademicTrendQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);
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

        var attempts = await attemptsQuery
            .OrderBy(a => a.TakenAt)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);

        if (attempts.Count == 0) return [];

        var attemptIds = attempts.Select(a => a.Id).ToList();

        var subjectResults = await db.Set<StudentExamSubjectResult>()
            .AsNoTracking()
            .Where(sr => attemptIds.Contains(sr.StudentExamAttemptId))
            .ToListAsync(ct);

        if (subjectResults.Count == 0) return [];

        var examIds = attempts.Select(a => a.ExamId).Distinct().ToList();
        var exams = await db.Set<Exam>()
            .AsNoTracking()
            .Where(e => examIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        var sectionIds = subjectResults.Select(sr => sr.ExamSectionId).Distinct().ToList();
        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(sec => sectionIds.Contains(sec.Id))
            .ToDictionaryAsync(sec => sec.Id, ct);

        var subjectIds = sections.Values.Select(s => s.SubjectId).Distinct().ToList();
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        var attemptDict = attempts.ToDictionary(a => a.Id);

        // Group per (SubjectId, AttemptId) to guarantee ONE observation point per AttemptId + SubjectId
        var subjectAttemptGroups = subjectResults
            .Select(sr =>
            {
                var sec = sections.GetValueOrDefault(sr.ExamSectionId);
                var att = attemptDict.GetValueOrDefault(sr.StudentExamAttemptId);
                var exam = att != null ? exams.GetValueOrDefault(att.ExamId) : null;
                return new
                {
                    SubjectId = sec?.SubjectId ?? Guid.Empty,
                    Attempt = att,
                    Exam = exam,
                    Result = sr
                };
            })
            .Where(x => x.SubjectId != Guid.Empty && x.Attempt != null && x.Exam != null)
            .GroupBy(x => new { x.SubjectId, x.Attempt!.Id });

        var pointsBySubject = subjectAttemptGroups
            .Select(g =>
            {
                var subjectId = g.Key.SubjectId;
                var attempt = g.First().Attempt!;
                var exam = g.First().Exam!;

                var correct = g.Sum(x => x.Result.Correct);
                var wrong = g.Sum(x => x.Result.Wrong);
                var blank = g.Sum(x => x.Result.Blank);
                var net = g.Sum(x => x.Result.Net);
                var totalQ = correct + wrong + blank;
                var attempted = correct + wrong;

                var accuracy = attempted > 0 ? Math.Round((decimal)correct / attempted, 4) : 0m;
                var successRate = totalQ > 0 ? Math.Round((decimal)correct / totalQ, 4) : 0m;
                var coverage = totalQ > 0 ? Math.Round((decimal)attempted / totalQ, 4) : 0m;
                var netPerQ = totalQ > 0 ? Math.Round(net / totalQ, 4) : 0m;

                return new
                {
                    SubjectId = subjectId,
                    Attempt = attempt,
                    Point = new SubjectObservationPointDto(
                        AttemptId: attempt.Id,
                        ExamId: exam.Id,
                        ExamTitle: exam.Title,
                        TakenAt: attempt.TakenAt,
                        AttemptNumber: attempt.AttemptNumber,
                        SubjectId: subjectId,
                        TotalQuestions: totalQ,
                        Correct: correct,
                        Wrong: wrong,
                        Blank: blank,
                        Net: net,
                        NetPerQuestion: netPerQ,
                        Accuracy: accuracy,
                        SuccessRate: successRate,
                        Coverage: coverage
                    )
                };
            })
            .GroupBy(x => x.SubjectId);

        var response = new List<SubjectTrendDto>();

        foreach (var group in pointsBySubject)
        {
            var subjectId = group.Key;
            var subjectName = subjectNames.GetValueOrDefault(subjectId, "Bilinmeyen Ders");

            var points = group
                .OrderBy(x => x.Attempt.TakenAt)
                .ThenBy(x => x.Attempt.Id)
                .Select(x => x.Point)
                .ToList();

            var trendDto = AcademicTrendCalculator.CalculateSubjectTrend(
                subjectId,
                subjectName,
                points,
                query.WindowSize,
                query.Limit
            );

            response.Add(trendDto);
        }

        return response;
    }

    public async Task<List<TopicTrendDto>> GetTopicTrendsAsync(
        AcademicTrendQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);
        await resourceAccess.ValidateStudentReadAccessAsync(query.StudentId, ct);

        // RAW attempts ONLY (IsRawAvailable == true) — NEVER fabricate topic trends from aggregate attempts
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

        var attempts = await attemptsQuery
            .OrderBy(a => a.TakenAt)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);

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

        var examIds = attempts.Select(a => a.ExamId).Distinct().ToList();
        var exams = await db.Set<Exam>()
            .AsNoTracking()
            .Where(e => examIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        var attemptDict = attempts.ToDictionary(a => a.Id);

        // Group per (TopicId, AttemptId) to guarantee ONE observation point per AttemptId + TopicId
        var topicAttemptGroups = topicResults
            .Select(tr =>
            {
                var sec = sections.GetValueOrDefault(tr.ExamSectionId);
                var att = attemptDict.GetValueOrDefault(tr.StudentExamAttemptId);
                var exam = att != null ? exams.GetValueOrDefault(att.ExamId) : null;
                return new
                {
                    TopicId = tr.TopicId,
                    SubjectId = sec?.SubjectId ?? Guid.Empty,
                    Attempt = att,
                    Exam = exam,
                    Result = tr
                };
            })
            .Where(x => x.Attempt != null && x.Exam != null)
            .GroupBy(x => new { x.TopicId, x.Attempt!.Id });

        var pointsByTopic = topicAttemptGroups
            .Select(g =>
            {
                var topicId = g.Key.TopicId;
                var subjectId = g.First().SubjectId;
                var attempt = g.First().Attempt!;
                var exam = g.First().Exam!;

                var correct = g.Sum(x => x.Result.Correct);
                var wrong = g.Sum(x => x.Result.Wrong);
                var blank = g.Sum(x => x.Result.Blank);
                var net = g.Sum(x => x.Result.Net);
                var totalQ = correct + wrong + blank;
                var attempted = correct + wrong;

                var accuracy = attempted > 0 ? Math.Round((decimal)correct / attempted, 4) : 0m;
                var successRate = totalQ > 0 ? Math.Round((decimal)correct / totalQ, 4) : 0m;
                var coverage = totalQ > 0 ? Math.Round((decimal)attempted / totalQ, 4) : 0m;
                var netPerQ = totalQ > 0 ? Math.Round(net / totalQ, 4) : 0m;

                return new
                {
                    TopicId = topicId,
                    SubjectId = subjectId,
                    Attempt = attempt,
                    Point = new TopicObservationPointDto(
                        AttemptId: attempt.Id,
                        ExamId: exam.Id,
                        ExamTitle: exam.Title,
                        TakenAt: attempt.TakenAt,
                        AttemptNumber: attempt.AttemptNumber,
                        TopicId: topicId,
                        TotalQuestions: totalQ,
                        Correct: correct,
                        Wrong: wrong,
                        Blank: blank,
                        Net: net,
                        NetPerQuestion: netPerQ,
                        Accuracy: accuracy,
                        SuccessRate: successRate,
                        Coverage: coverage
                    )
                };
            })
            .GroupBy(x => x.TopicId);

        var response = new List<TopicTrendDto>();

        foreach (var group in pointsByTopic)
        {
            var topicId = group.Key;
            var topicName = topicNames.GetValueOrDefault(topicId, "Bilinmeyen Konu");
            var subjectId = group.First().SubjectId;
            var subjectName = subjectNames.GetValueOrDefault(subjectId, "Bilinmeyen Ders");

            var points = group
                .OrderBy(x => x.Attempt.TakenAt)
                .ThenBy(x => x.Attempt.Id)
                .Select(x => x.Point)
                .ToList();

            var trendDto = AcademicTrendCalculator.CalculateTopicTrend(
                topicId,
                topicName,
                subjectId,
                subjectName,
                points,
                query.WindowSize,
                query.Limit
            );

            response.Add(trendDto);
        }

        return response;
    }

    public async Task<List<LearningOutcomeTrendDto>> GetLearningOutcomeTrendsAsync(
        AcademicTrendQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);
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

        var attempts = await attemptsQuery
            .OrderBy(a => a.TakenAt)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);

        if (attempts.Count == 0) return [];

        var attemptIds = attempts.Select(a => a.Id).ToList();

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

        var examIds = attempts.Select(a => a.ExamId).Distinct().ToList();
        var exams = await db.Set<Exam>()
            .AsNoTracking()
            .Where(e => examIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        var attemptDict = attempts.ToDictionary(a => a.Id);

        var loAttemptGroups = rawQuestionResults
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
            .GroupBy(x => new { x.Question!.LearningOutcomeId!.Value, x.Attempt!.Id });

        var pointsByLo = loAttemptGroups
            .Select(g =>
            {
                var loId = g.Key.Value;
                var attempt = g.First().Attempt!;
                var exam = exams.GetValueOrDefault(attempt.ExamId);
                var sampleQ = g.First().Question!;

                var totalQ = g.Count();
                var correct = g.Count(x => x.QuestionResult.IsCorrect == true && !x.QuestionResult.IsBlank);
                var wrong = g.Count(x => x.QuestionResult.IsCorrect == false && !x.QuestionResult.IsBlank);
                var blank = g.Count(x => x.QuestionResult.IsBlank || x.QuestionResult.SelectedOption == null);
                var attempted = correct + wrong;

                var accuracy = attempted > 0 ? Math.Round((decimal)correct / attempted, 4) : 0m;
                var successRate = totalQ > 0 ? Math.Round((decimal)correct / totalQ, 4) : 0m;
                var coverage = totalQ > 0 ? Math.Round((decimal)attempted / totalQ, 4) : 0m;

                return new
                {
                    LearningOutcomeId = loId,
                    TopicId = sampleQ.TopicId,
                    SubjectId = sampleQ.SubjectId,
                    Attempt = attempt,
                    Exam = exam,
                    Point = new LearningOutcomeObservationPointDto(
                        AttemptId: attempt.Id,
                        ExamId: exam?.Id ?? Guid.Empty,
                        ExamTitle: exam?.Title ?? string.Empty,
                        TakenAt: attempt.TakenAt,
                        AttemptNumber: attempt.AttemptNumber,
                        LearningOutcomeId: loId,
                        TotalQuestions: totalQ,
                        Correct: correct,
                        Wrong: wrong,
                        Blank: blank,
                        Accuracy: accuracy,
                        SuccessRate: successRate,
                        Coverage: coverage
                    )
                };
            })
            .Where(x => x.Exam != null)
            .GroupBy(x => x.LearningOutcomeId);

        var response = new List<LearningOutcomeTrendDto>();

        foreach (var group in pointsByLo)
        {
            var loId = group.Key;
            var lo = learningOutcomes.GetValueOrDefault(loId);
            var loCode = lo?.Code ?? "KAZANIM";
            var loTitle = lo?.Name ?? "Bilinmeyen Kazanım";

            var firstItem = group.First();
            var topicId = firstItem.TopicId;
            var topicName = topicNames.GetValueOrDefault(topicId, "Bilinmeyen Konu");
            var subjectId = firstItem.SubjectId;
            var subjectName = subjectNames.GetValueOrDefault(subjectId, "Bilinmeyen Ders");

            var points = group
                .OrderBy(x => x.Attempt.TakenAt)
                .ThenBy(x => x.Attempt.Id)
                .Select(x => x.Point)
                .ToList();

            var trendDto = AcademicTrendCalculator.CalculateLearningOutcomeTrend(
                loId,
                loCode,
                loTitle,
                topicId,
                topicName,
                subjectId,
                subjectName,
                points,
                query.WindowSize,
                query.Limit
            );

            response.Add(trendDto);
        }

        return response;
    }
}
