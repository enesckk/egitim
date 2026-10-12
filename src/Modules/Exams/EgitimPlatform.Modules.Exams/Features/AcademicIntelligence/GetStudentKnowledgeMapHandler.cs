using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Questions.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public class GetStudentKnowledgeMapHandler(
    IApplicationDbContext db,
    IExamResourceAccess resourceAccess,
    GetStudentAcademicClassificationHandler classificationHandler)
{
    private static void ValidateQueryParameters(AcademicClassificationQuery query)
    {
        if (query.WindowSize < 2 || query.WindowSize > 10)
            throw new ValidationException("WindowSize must be between 2 and 10.");

        if (query.Limit.HasValue && (query.Limit.Value < 1 || query.Limit.Value > 100))
            throw new ValidationException("Limit must be between 1 and 100.");

        if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value)
            throw new ValidationException("From date cannot be after To date.");
    }

    public async Task<StudentKnowledgeMapDto> GetKnowledgeMapAsync(
        AcademicClassificationQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);
        await resourceAccess.ValidateStudentReadAccessAsync(query.StudentId, ct);

        // 1. Determine relevant ExamTypes scope
        List<Guid> targetExamTypeIds = [];
        if (query.ExamTypeId.HasValue)
        {
            targetExamTypeIds.Add(query.ExamTypeId.Value);
        }
        else
        {
            // Derive relevant ExamTypeIds from student's finalized attempts within request filters
            var attemptsQ = db.Set<StudentExamAttempt>()
                .AsNoTracking()
                .Where(a => a.StudentId == query.StudentId && a.Status == AttemptStatus.Finalized);

            if (query.From.HasValue) attemptsQ = attemptsQ.Where(a => a.TakenAt >= query.From.Value);
            if (query.To.HasValue) attemptsQ = attemptsQ.Where(a => a.TakenAt <= query.To.Value);

            var studentExamIds = await attemptsQ.Select(a => a.ExamId).Distinct().ToListAsync(ct);
            if (studentExamIds.Count == 0)
            {
                // No evidence and no explicit ExamTypeId -> return empty map
                var emptySummary = new KnowledgeMapSummaryDto(0, 0, 0, 0, 0, 0, 0, 0, 0);
                return new StudentKnowledgeMapDto(
                    query.StudentId,
                    AcademicClassificationPolicy.CurrentPolicyVersion,
                    query.ExamTypeId,
                    emptySummary,
                    []
                );
            }

            targetExamTypeIds = await db.Set<Exam>()
                .AsNoTracking()
                .Where(e => studentExamIds.Contains(e.Id))
                .Select(e => e.ExamTypeId)
                .Distinct()
                .ToListAsync(ct);
        }

        // 2. Load Classifications set-wise
        var subjectClassifications = (await classificationHandler.GetSubjectClassificationsAsync(query, ct))
            .ToDictionary(c => c.SubjectId);

        var topicClassifications = (await classificationHandler.GetTopicClassificationsAsync(query, ct))
            .ToDictionary(c => c.TopicId);

        var loClassifications = (await classificationHandler.GetLearningOutcomeClassificationsAsync(query, ct))
            .ToDictionary(c => c.LearningOutcomeId);

        // 3. Find direct SubTopic evidence from raw question results
        var rawAttemptsQ = db.Set<StudentExamAttempt>()
            .AsNoTracking()
            .Where(a => a.StudentId == query.StudentId && a.Status == AttemptStatus.Finalized && a.IsRawAvailable);

        if (query.From.HasValue) rawAttemptsQ = rawAttemptsQ.Where(a => a.TakenAt >= query.From.Value);
        if (query.To.HasValue) rawAttemptsQ = rawAttemptsQ.Where(a => a.TakenAt <= query.To.Value);

        var rawAttemptIds = await rawAttemptsQ.Select(a => a.Id).ToListAsync(ct);
        HashSet<Guid> directEvidenceSubTopicIds = [];

        if (rawAttemptIds.Count > 0)
        {
            var questionIds = await db.Set<StudentExamQuestionResult>()
                .AsNoTracking()
                .Where(qr => rawAttemptIds.Contains(qr.StudentExamAttemptId))
                .Select(qr => qr.QuestionId)
                .Distinct()
                .ToListAsync(ct);

            if (questionIds.Count > 0)
            {
                var subTopicIdsFromQ = await db.Set<Question>()
                    .AsNoTracking()
                    .Where(q => questionIds.Contains(q.Id) && q.SubTopicId.HasValue)
                    .Select(q => q.SubTopicId!.Value)
                    .Distinct()
                    .ToListAsync(ct);

                directEvidenceSubTopicIds = [.. subTopicIdsFromQ];
            }
        }

        // 4. Load Taxonomy set-wise (active + historical evidence-backed)
        var activeSubjects = await db.Set<Subject>()
            .AsNoTracking()
            .Where(s => targetExamTypeIds.Contains(s.ExamTypeId))
            .ToListAsync(ct);

        var activeSubjectIds = activeSubjects.Select(s => s.Id).ToHashSet();

        // Historical preservation: soft-deleted subjects referenced in evidence
        var extraSubjectIds = subjectClassifications.Keys.Except(activeSubjectIds).ToList();
        List<Subject> historicalSubjects = [];
        if (extraSubjectIds.Count > 0)
        {
            // Tenant safety: strictly constrained to evidence-backed extraSubjectIds
            historicalSubjects = await db.Set<Subject>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => extraSubjectIds.Contains(s.Id))
                .ToListAsync(ct);
        }

        var allSubjects = activeSubjects.Concat(historicalSubjects)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .OrderBy(s => s.Code)
            .ThenBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToList();

        var allSubjectIds = allSubjects.Select(s => s.Id).ToList();

        var activeTopics = await db.Set<Topic>()
            .AsNoTracking()
            .Where(t => allSubjectIds.Contains(t.SubjectId))
            .ToListAsync(ct);

        var activeTopicIds = activeTopics.Select(t => t.Id).ToHashSet();
        var extraTopicIds = topicClassifications.Keys.Except(activeTopicIds).ToList();
        List<Topic> historicalTopics = [];
        if (extraTopicIds.Count > 0)
        {
            historicalTopics = await db.Set<Topic>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => extraTopicIds.Contains(t.Id))
                .ToListAsync(ct);
        }

        var allTopics = activeTopics.Concat(historicalTopics)
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .OrderBy(t => t.Code)
            .ThenBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToList();

        var allTopicIds = allTopics.Select(t => t.Id).ToList();

        var activeSubTopics = await db.Set<SubTopic>()
            .AsNoTracking()
            .Where(st => allTopicIds.Contains(st.TopicId))
            .ToListAsync(ct);

        var activeSubTopicIds = activeSubTopics.Select(st => st.Id).ToHashSet();
        var extraSubTopicIds = directEvidenceSubTopicIds.Except(activeSubTopicIds).ToList();
        List<SubTopic> historicalSubTopics = [];
        if (extraSubTopicIds.Count > 0)
        {
            historicalSubTopics = await db.Set<SubTopic>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(st => extraSubTopicIds.Contains(st.Id))
                .ToListAsync(ct);
        }

        var allSubTopics = activeSubTopics.Concat(historicalSubTopics)
            .GroupBy(st => st.Id)
            .Select(g => g.First())
            .OrderBy(st => st.Code)
            .ThenBy(st => st.Name)
            .ThenBy(st => st.Id)
            .ToList();

        var allSubTopicIds = allSubTopics.Select(st => st.Id).ToList();

        var activeLOs = await db.Set<LearningOutcome>()
            .AsNoTracking()
            .Where(lo => allSubTopicIds.Contains(lo.SubTopicId))
            .ToListAsync(ct);

        var activeLOIds = activeLOs.Select(lo => lo.Id).ToHashSet();
        var extraLOIds = loClassifications.Keys.Except(activeLOIds).ToList();
        List<LearningOutcome> historicalLOs = [];
        if (extraLOIds.Count > 0)
        {
            historicalLOs = await db.Set<LearningOutcome>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(lo => extraLOIds.Contains(lo.Id))
                .ToListAsync(ct);
        }

        var allLearningOutcomes = activeLOs.Concat(historicalLOs)
            .GroupBy(lo => lo.Id)
            .Select(g => g.First())
            .OrderBy(lo => lo.Code)
            .ThenBy(lo => lo.Name)
            .ThenBy(lo => lo.Id)
            .ToList();

        // Dictionaries for fast parent resolution
        var subTopicDict = allSubTopics.ToDictionary(st => st.Id);
        var topicDict = allTopics.ToDictionary(t => t.Id);
        var subjectDict = allSubjects.ToDictionary(s => s.Id);

        // 5. Build DTOs
        // LearningOutcome Nodes
        var loNodesBySubTopic = allLearningOutcomes
            .GroupBy(lo => lo.SubTopicId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(lo =>
                {
                    var st = subTopicDict.GetValueOrDefault(lo.SubTopicId);
                    var t = st != null ? topicDict.GetValueOrDefault(st.TopicId) : null;
                    var s = t != null ? subjectDict.GetValueOrDefault(t.SubjectId) : null;
                    var cls = loClassifications.GetValueOrDefault(lo.Id);

                    bool hasEvidence = cls != null && cls.ObservationCount > 0;
                    bool hasEnoughData = cls?.HasEnoughData ?? false;
                    var state = cls?.State ?? AcademicClassificationState.InsufficientData;
                    var basis = cls?.Basis ?? ClassificationBasis.OverallHistory;
                    bool isOrphaned = st == null || t == null || s == null;

                    return new KnowledgeMapLearningOutcomeNodeDto(
                        Id: lo.Id,
                        Code: lo.Code,
                        Name: lo.Name,
                        SubTopicId: st?.Id,
                        TopicId: t?.Id,
                        SubjectId: s?.Id,
                        State: state,
                        HasEnoughData: hasEnoughData,
                        HasEvidence: hasEvidence,
                        Basis: basis,
                        ObservationCount: cls?.ObservationCount ?? 0,
                        TotalQuestions: cls?.TotalQuestions ?? 0,
                        AttemptedCount: cls?.AttemptedCount ?? 0,
                        TotalCorrect: cls?.TotalCorrect ?? 0,
                        TotalWrong: cls?.TotalWrong ?? 0,
                        TotalBlank: cls?.TotalBlank ?? 0,
                        SuccessRate: hasEvidence ? cls?.SuccessRate : null,
                        Accuracy: hasEvidence ? cls?.Accuracy : null,
                        Coverage: hasEvidence ? cls?.Coverage : null,
                        TrendDirection: cls?.TrendDirection,
                        SuccessRateDelta: cls?.SuccessRateDelta,
                        IsOrphaned: isOrphaned
                    );
                })
                .OrderBy(n => n.Code)
                .ThenBy(n => n.Name)
                .ThenBy(n => n.Id)
                .ToList()
            );

        // SubTopic Nodes
        var subTopicNodesByTopic = allSubTopics
            .GroupBy(st => st.TopicId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(st =>
                {
                    var childLos = loNodesBySubTopic.GetValueOrDefault(st.Id, []);
                    bool hasDirectEvidence = directEvidenceSubTopicIds.Contains(st.Id);
                    bool hasEvidence = hasDirectEvidence || childLos.Any(l => l.HasEvidence);
                    var t = topicDict.GetValueOrDefault(st.TopicId);
                    bool isOrphaned = t == null;

                    return new KnowledgeMapSubTopicNodeDto(
                        Id: st.Id,
                        Code: st.Code,
                        Name: st.Name,
                        TopicId: t?.Id,
                        State: AcademicClassificationState.InsufficientData,
                        HasEnoughData: false,
                        HasEvidence: hasEvidence,
                        Basis: ClassificationBasis.OverallHistory,
                        ObservationCount: childLos.Sum(l => l.ObservationCount),
                        TotalQuestions: childLos.Sum(l => l.TotalQuestions),
                        AttemptedCount: childLos.Sum(l => l.AttemptedCount),
                        TotalCorrect: childLos.Sum(l => l.TotalCorrect),
                        TotalWrong: childLos.Sum(l => l.TotalWrong),
                        TotalBlank: childLos.Sum(l => l.TotalBlank),
                        SuccessRate: null,
                        Accuracy: null,
                        Coverage: null,
                        TrendDirection: null,
                        SuccessRateDelta: null,
                        LearningOutcomes: childLos,
                        IsOrphaned: isOrphaned
                    );
                })
                .OrderBy(n => n.Code)
                .ThenBy(n => n.Name)
                .ThenBy(n => n.Id)
                .ToList()
            );

        // Topic Nodes
        var topicNodesBySubject = allTopics
            .GroupBy(t => t.SubjectId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t =>
                {
                    var childSubTopics = subTopicNodesByTopic.GetValueOrDefault(t.Id, []);
                    var cls = topicClassifications.GetValueOrDefault(t.Id);
                    var s = subjectDict.GetValueOrDefault(t.SubjectId);

                    bool hasEvidence = (cls != null && cls.ObservationCount > 0) || childSubTopics.Any(st => st.HasEvidence);
                    bool hasEnoughData = cls?.HasEnoughData ?? false;
                    var state = cls?.State ?? AcademicClassificationState.InsufficientData;
                    var basis = cls?.Basis ?? ClassificationBasis.OverallHistory;
                    bool isOrphaned = s == null;

                    return new KnowledgeMapTopicNodeDto(
                        Id: t.Id,
                        Code: t.Code,
                        Name: t.Name,
                        SubjectId: s?.Id,
                        State: state,
                        HasEnoughData: hasEnoughData,
                        HasEvidence: hasEvidence,
                        Basis: basis,
                        ObservationCount: cls?.ObservationCount ?? 0,
                        TotalQuestions: cls?.TotalQuestions ?? 0,
                        AttemptedCount: cls?.AttemptedCount ?? 0,
                        TotalCorrect: cls?.TotalCorrect ?? 0,
                        TotalWrong: cls?.TotalWrong ?? 0,
                        TotalBlank: cls?.TotalBlank ?? 0,
                        SuccessRate: hasEvidence ? cls?.SuccessRate : null,
                        Accuracy: hasEvidence ? cls?.Accuracy : null,
                        Coverage: hasEvidence ? cls?.Coverage : null,
                        TrendDirection: cls?.TrendDirection,
                        SuccessRateDelta: cls?.SuccessRateDelta,
                        SubTopics: childSubTopics,
                        IsOrphaned: isOrphaned
                    );
                })
                .OrderBy(n => n.Code)
                .ThenBy(n => n.Name)
                .ThenBy(n => n.Id)
                .ToList()
            );

        // Subject Nodes
        var subjectNodes = allSubjects
            .Select(s =>
            {
                var childTopics = topicNodesBySubject.GetValueOrDefault(s.Id, []);
                var cls = subjectClassifications.GetValueOrDefault(s.Id);

                bool hasEvidence = (cls != null && cls.ObservationCount > 0) || childTopics.Any(t => t.HasEvidence);
                bool hasEnoughData = cls?.HasEnoughData ?? false;
                var state = cls?.State ?? AcademicClassificationState.InsufficientData;
                var basis = cls?.Basis ?? ClassificationBasis.OverallHistory;

                return new KnowledgeMapSubjectNodeDto(
                    Id: s.Id,
                    Code: s.Code,
                    Name: s.Name,
                    State: state,
                    HasEnoughData: hasEnoughData,
                    HasEvidence: hasEvidence,
                    Basis: basis,
                    ObservationCount: cls?.ObservationCount ?? 0,
                    TotalQuestions: cls?.TotalQuestions ?? 0,
                    AttemptedCount: cls?.AttemptedCount ?? 0,
                    TotalCorrect: cls?.TotalCorrect ?? 0,
                    TotalWrong: cls?.TotalWrong ?? 0,
                    TotalBlank: cls?.TotalBlank ?? 0,
                    SuccessRate: hasEvidence ? cls?.SuccessRate : null,
                    Accuracy: hasEvidence ? cls?.Accuracy : null,
                    Coverage: hasEvidence ? cls?.Coverage : null,
                    TrendDirection: cls?.TrendDirection,
                    SuccessRateDelta: cls?.SuccessRateDelta,
                    Topics: childTopics
                );
            })
            .OrderBy(s => s.Code)
            .ThenBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToList();

        // Summary Calculations
        var summary = new KnowledgeMapSummaryDto(
            TotalSubjects: subjectNodes.Count,
            SubjectsWithEvidence: subjectNodes.Count(s => s.HasEvidence),
            StrongSubjects: subjectNodes.Count(s => s.State == AcademicClassificationState.Strong),
            WeakSubjects: subjectNodes.Count(s => s.State == AcademicClassificationState.Weak),
            CriticalSubjects: subjectNodes.Count(s => s.State == AcademicClassificationState.Critical),
            TotalTopics: allTopics.Count,
            TopicsWithEvidence: topicNodesBySubject.Values.SelectMany(x => x).Count(t => t.HasEvidence),
            TotalLearningOutcomes: allLearningOutcomes.Count,
            LearningOutcomesWithEvidence: loNodesBySubTopic.Values.SelectMany(x => x).Count(lo => lo.HasEvidence)
        );

        return new StudentKnowledgeMapDto(
            StudentId: query.StudentId,
            PolicyVersion: AcademicClassificationPolicy.CurrentPolicyVersion,
            ExamTypeId: query.ExamTypeId,
            Summary: summary,
            Subjects: subjectNodes
        );
    }
}
