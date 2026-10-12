using System.Security.Cryptography;
using System.Text;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.AI.Features;

public class GetStudentAcademicAiSummaryHandler(
    IExamResourceAccess resourceAccess,
    GetStudentAcademicClassificationHandler classificationHandler,
    GetStudentAcademicTrendHandler trendHandler,
    GetStudentAcademicRecommendationHandler recommendationHandler,
    IAcademicInterpretationService interpretationService,
    IApplicationDbContext db)
{
    public async Task<AcademicAiSummaryDto> HandleAsync(
        Guid studentId,
        string audience = "student",
        CancellationToken ct = default)
    {
        await resourceAccess.ValidateStudentReadAccessAsync(studentId, ct);

        var classQuery = new AcademicClassificationQuery(studentId);
        var topicClassifications = await classificationHandler.GetTopicClassificationsAsync(classQuery, ct);

        var trendQuery = new AcademicTrendQuery(studentId);
        var subjectTrends = await trendHandler.GetSubjectTrendsAsync(trendQuery, ct);

        var recQuery = new AcademicRecommendationQuery(studentId);
        var recommendations = await recommendationHandler.GetRecommendationsAsync(recQuery, ct);

        var plan = await db.Set<StudyPlan>()
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

        var rawFingerprintInput = new StringBuilder();
        rawFingerprintInput.Append($"{studentId}:{audience}:");
        foreach (var ft in factTopics) rawFingerprintInput.Append($"{ft.TopicId}_{ft.ClassificationState}_{ft.SuccessRate};");
        foreach (var tr in factTrends) rawFingerprintInput.Append($"{tr.SubjectName}_{tr.TrendDirection};");
        foreach (var rc in factRecs) rawFingerprintInput.Append($"{rc.Title}_{rc.Priority};");
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
            FactFingerprint: fingerprint
        );

        return await interpretationService.GenerateInterpretationAsync(factPackage, ct);
    }
}
