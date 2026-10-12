using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;

namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public class GetStudentAcademicStudyPlanHandler(
    IExamResourceAccess resourceAccess,
    GetStudentAcademicRecommendationHandler recommendationHandler)
{
    private static void ValidateQueryParameters(AcademicStudyPlanQuery query)
    {
        if (query.PlanDays < AcademicStudyPlanPolicy.MinPlanDays ||
            query.PlanDays > AcademicStudyPlanPolicy.MaxPlanDays)
        {
            throw new ValidationException($"PlanDays must be between {AcademicStudyPlanPolicy.MinPlanDays} and {AcademicStudyPlanPolicy.MaxPlanDays}.");
        }

        if (query.DailyStudyMinutes < AcademicStudyPlanPolicy.MinDailyStudyMinutes ||
            query.DailyStudyMinutes > AcademicStudyPlanPolicy.MaxDailyStudyMinutes)
        {
            throw new ValidationException($"DailyStudyMinutes must be between {AcademicStudyPlanPolicy.MinDailyStudyMinutes} and {AcademicStudyPlanPolicy.MaxDailyStudyMinutes}.");
        }

        if (query.RecommendationLimit < AcademicRecommendationPolicy.MinRecommendationLimit ||
            query.RecommendationLimit > AcademicRecommendationPolicy.MaxRecommendationLimit)
        {
            throw new ValidationException($"RecommendationLimit must be between {AcademicRecommendationPolicy.MinRecommendationLimit} and {AcademicRecommendationPolicy.MaxRecommendationLimit}.");
        }

        if (query.WindowSize < 2 || query.WindowSize > 10)
            throw new ValidationException("WindowSize must be between 2 and 10.");

        if (query.Limit.HasValue && (query.Limit.Value < 1 || query.Limit.Value > 100))
            throw new ValidationException("Limit must be between 1 and 100.");

        if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value)
            throw new ValidationException("From date cannot be after To date.");
    }

    public virtual async Task<AcademicStudyPlanDto> GetStudyPlanAsync(
        AcademicStudyPlanQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);
        await resourceAccess.ValidateStudentReadAccessAsync(query.StudentId, ct);

        var recQuery = new AcademicRecommendationQuery(
            query.StudentId,
            query.ExamTypeId,
            query.WindowSize,
            query.Limit,
            query.RecommendationLimit,
            query.From,
            query.To
        );

        var recommendations = await recommendationHandler.GetRecommendationsAsync(recQuery, ct);

        return AcademicStudyPlanCalculator.CalculateStudyPlan(
            query.StudentId,
            recommendations,
            query.PlanDays,
            query.DailyStudyMinutes,
            query.StartDate
        );
    }
}
