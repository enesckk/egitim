using System.Security.Cryptography;
using System.Text;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Modules.AI.Features;

public class GetStudentAcademicAiSummaryHandler
{
    private readonly IExamResourceAccess _resourceAccess;
    private readonly GetStudentAcademicClassificationHandler _classificationHandler;
    private readonly GetStudentAcademicTrendHandler _trendHandler;
    private readonly GetStudentAcademicRecommendationHandler _recommendationHandler;
    private readonly IAcademicInterpretationService _interpretationService;
    private readonly IApplicationDbContext _db;
    private readonly GetStudentQuestionStyleAnalyticsHandler? _styleAnalyticsHandler;

    public GetStudentAcademicAiSummaryHandler(
        IExamResourceAccess resourceAccess,
        GetStudentAcademicClassificationHandler classificationHandler,
        GetStudentAcademicTrendHandler trendHandler,
        GetStudentAcademicRecommendationHandler recommendationHandler,
        IAcademicInterpretationService interpretationService,
        IApplicationDbContext db)
        : this(resourceAccess, classificationHandler, trendHandler, recommendationHandler, interpretationService, db, null)
    {
    }

    [ActivatorUtilitiesConstructor]
    public GetStudentAcademicAiSummaryHandler(
        IExamResourceAccess resourceAccess,
        GetStudentAcademicClassificationHandler classificationHandler,
        GetStudentAcademicTrendHandler trendHandler,
        GetStudentAcademicRecommendationHandler recommendationHandler,
        IAcademicInterpretationService interpretationService,
        IApplicationDbContext db,
        GetStudentQuestionStyleAnalyticsHandler? styleAnalyticsHandler)
    {
        _resourceAccess = resourceAccess;
        _classificationHandler = classificationHandler;
        _trendHandler = trendHandler;
        _recommendationHandler = recommendationHandler;
        _interpretationService = interpretationService;
        _db = db;
        _styleAnalyticsHandler = styleAnalyticsHandler;
    }

    public async Task<AcademicAiSummaryDto> HandleAsync(
        Guid studentId,
        string audience = "student",
        CancellationToken ct = default)
    {
        await _resourceAccess.ValidateStudentReadAccessAsync(studentId, ct);

        var classQuery = new AcademicClassificationQuery(studentId);
        var topicClassifications = await _classificationHandler.GetTopicClassificationsAsync(classQuery, ct);

        var trendQuery = new AcademicTrendQuery(studentId);
        var subjectTrends = await _trendHandler.GetSubjectTrendsAsync(trendQuery, ct);

        var recQuery = new AcademicRecommendationQuery(studentId);
        var recommendations = await _recommendationHandler.GetRecommendationsAsync(recQuery, ct);

        var plan = await _db.Set<StudyPlan>()
            .AsNoTracking()
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.Status == StudyPlanStatus.Active && !p.IsDeleted, ct);

        AcademicFactPlanSummary? planSummary = null;
        if (plan != null)
        {
            var activeTasks = plan.Tasks.Where(t => !t.IsDeleted).ToList();
            var completedCount = activeTasks.Count(t => t.IsCompleted);
            planSummary = new AcademicFactPlanSummary(
                Status: plan.Status.ToString(),
                CompletedTaskCount: completedCount,
                TotalTaskCount: activeTasks.Count,
                UtilizationRate: plan.UtilizationRate
            );
        }

        var factTopics = topicClassifications.Select(tc => new AcademicFactTopic(
            TopicId: tc.TopicId,
            TopicName: tc.TopicName,
            ClassificationState: tc.State.ToString(),
            NetRate: tc.Accuracy,
            SuccessRate: tc.SuccessRate
        )).ToList();

        var factTrends = subjectTrends.Select(st => new AcademicFactTrend(
            SubjectName: st.SubjectName,
            TrendDirection: st.Direction?.ToString() ?? "Stable",
            Message: $"{st.SubjectName} genel eğilimi: {st.Direction?.ToString() ?? "Sabit"}"
        )).ToList();

        var factRecs = recommendations.Select(r => new AcademicFactRecommendation(
            Title: r.Name,
            RecommendationType: r.RecommendationType.ToString(),
            Priority: r.PriorityScore
        )).ToList();

        var factStyles = new List<AcademicFactQuestionStyle>();
        if (_styleAnalyticsHandler != null)
        {
            var styleAnalytics = await _styleAnalyticsHandler.HandleAsync(new AcademicIntelligenceQuery(studentId), ct);
            factStyles = styleAnalytics
                .Where(s => s.ClassificationState != AcademicClassificationState.InsufficientData)
                .OrderBy(s => s.Code, StringComparer.Ordinal)
                .ThenBy(s => s.QuestionStyleId)
                .Select(s => new AcademicFactQuestionStyle(
                    QuestionStyleId: s.QuestionStyleId,
                    Code: s.Code,
                    Name: s.Name,
                    ClassificationState: s.ClassificationState.ToString(),
                    SuccessRate: s.SuccessRate,
                    TrendDirection: s.TrendDirection?.ToString()
                )).ToList();
        }

        var rawFingerprintInput = new StringBuilder();
        rawFingerprintInput.Append($"{studentId}:{audience}:");
        foreach (var ft in factTopics) rawFingerprintInput.Append($"{ft.TopicId}_{ft.ClassificationState}_{ft.SuccessRate};");
        foreach (var tr in factTrends) rawFingerprintInput.Append($"{tr.SubjectName}_{tr.TrendDirection};");
        foreach (var rc in factRecs) rawFingerprintInput.Append($"{rc.Title}_{rc.Priority};");
        foreach (var qs in factStyles) rawFingerprintInput.Append($"{qs.QuestionStyleId}_{qs.ClassificationState}_{qs.SuccessRate}_{qs.TrendDirection};");
        if (planSummary != null) rawFingerprintInput.Append($"{planSummary.CompletedTaskCount}_{planSummary.TotalTaskCount}_{planSummary.UtilizationRate};");

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawFingerprintInput.ToString()));
        var fingerprint = Convert.ToHexString(hashBytes);

        var factPackage = new AcademicFactPackage(
            StudentId: studentId,
            Audience: audience,
            Classifications: factTopics,
            Trends: factTrends,
            Recommendations: factRecs,
            PlanSummary: planSummary,
            FactFingerprint: fingerprint,
            QuestionStyles: factStyles
        );

        return await _interpretationService.GenerateInterpretationAsync(factPackage, ct);
    }
}
