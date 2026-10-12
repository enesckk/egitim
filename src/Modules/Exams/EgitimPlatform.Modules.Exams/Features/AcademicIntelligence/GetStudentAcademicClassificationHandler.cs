using FluentValidation;

namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public class GetStudentAcademicClassificationHandler(GetStudentAcademicTrendHandler trendHandler)
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

    public async Task<List<SubjectClassificationDto>> GetSubjectClassificationsAsync(
        AcademicClassificationQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);

        var trendQuery = new AcademicTrendQuery(
            query.StudentId,
            query.ExamTypeId,
            query.WindowSize,
            query.Limit,
            query.From,
            query.To
        );

        var trends = await trendHandler.GetSubjectTrendsAsync(trendQuery, ct);
        return trends.Select(AcademicClassificationCalculator.CalculateSubjectClassification).ToList();
    }

    public async Task<List<TopicClassificationDto>> GetTopicClassificationsAsync(
        AcademicClassificationQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);

        var trendQuery = new AcademicTrendQuery(
            query.StudentId,
            query.ExamTypeId,
            query.WindowSize,
            query.Limit,
            query.From,
            query.To
        );

        var trends = await trendHandler.GetTopicTrendsAsync(trendQuery, ct);
        return trends.Select(AcademicClassificationCalculator.CalculateTopicClassification).ToList();
    }

    public async Task<List<LearningOutcomeClassificationDto>> GetLearningOutcomeClassificationsAsync(
        AcademicClassificationQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);

        var trendQuery = new AcademicTrendQuery(
            query.StudentId,
            query.ExamTypeId,
            query.WindowSize,
            query.Limit,
            query.From,
            query.To
        );

        var trends = await trendHandler.GetLearningOutcomeTrendsAsync(trendQuery, ct);
        return trends.Select(AcademicClassificationCalculator.CalculateLearningOutcomeClassification).ToList();
    }
}
