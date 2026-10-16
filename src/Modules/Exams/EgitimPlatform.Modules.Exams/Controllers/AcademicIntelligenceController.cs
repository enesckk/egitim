using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Exams.Controllers;

[ApiController]
[Route("api/v1/students/{studentId:guid}/academic-intelligence")]
[Authorize(Policy = Policies.CanViewExamResults)]
public class AcademicIntelligenceController(
    GetStudentAcademicIntelligenceHandler intelligenceHandler,
    GetStudentAcademicTrendHandler trendHandler,
    GetStudentAcademicClassificationHandler classificationHandler,
    GetStudentKnowledgeMapHandler mapHandler,
    GetStudentAcademicRecommendationHandler recommendationHandler,
    GetStudentAcademicStudyPlanHandler studyPlanHandler) : ControllerBase
{
    [HttpGet("subjects")]
    public async Task<ActionResult<List<StudentSubjectPerformanceDto>>> GetSubjectPerformance(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int? window,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var query = new AcademicIntelligenceQuery(studentId, examTypeId, window, from, to);
        var result = await intelligenceHandler.GetSubjectPerformanceAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("topics")]
    public async Task<ActionResult<List<StudentTopicPerformanceDto>>> GetTopicPerformance(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int? window,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var query = new AcademicIntelligenceQuery(studentId, examTypeId, window, from, to);
        var result = await intelligenceHandler.GetTopicPerformanceAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("learning-outcomes")]
    public async Task<ActionResult<List<StudentLearningOutcomePerformanceDto>>> GetLearningOutcomePerformance(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int? window,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var query = new AcademicIntelligenceQuery(studentId, examTypeId, window, from, to);
        var result = await intelligenceHandler.GetLearningOutcomePerformanceAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("coverage")]
    public async Task<ActionResult<AcademicCoverageSummaryDto>> GetCoverageSummary(
        [FromRoute] Guid studentId,
        CancellationToken ct)
    {
        var result = await intelligenceHandler.GetAcademicCoverageSummaryAsync(studentId, ct);
        return Ok(result);
    }

    [HttpGet("trends/subjects")]
    public async Task<ActionResult<List<SubjectTrendDto>>> GetSubjectTrends(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicTrendQuery(studentId, examTypeId, windowSize, limit, from, to);
        var result = await trendHandler.GetSubjectTrendsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("trends/topics")]
    public async Task<ActionResult<List<TopicTrendDto>>> GetTopicTrends(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicTrendQuery(studentId, examTypeId, windowSize, limit, from, to);
        var result = await trendHandler.GetTopicTrendsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("trends/learning-outcomes")]
    public async Task<ActionResult<List<LearningOutcomeTrendDto>>> GetLearningOutcomeTrends(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicTrendQuery(studentId, examTypeId, windowSize, limit, from, to);
        var result = await trendHandler.GetLearningOutcomeTrendsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("classifications/subjects")]
    public async Task<ActionResult<List<SubjectClassificationDto>>> GetSubjectClassifications(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicClassificationQuery(studentId, examTypeId, windowSize, limit, from, to);
        var result = await classificationHandler.GetSubjectClassificationsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("classifications/topics")]
    public async Task<ActionResult<List<TopicClassificationDto>>> GetTopicClassifications(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicClassificationQuery(studentId, examTypeId, windowSize, limit, from, to);
        var result = await classificationHandler.GetTopicClassificationsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("classifications/learning-outcomes")]
    public async Task<ActionResult<List<LearningOutcomeClassificationDto>>> GetLearningOutcomeClassifications(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicClassificationQuery(studentId, examTypeId, windowSize, limit, from, to);
        var result = await classificationHandler.GetLearningOutcomeClassificationsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("knowledge-map")]
    public async Task<ActionResult<StudentKnowledgeMapDto>> GetKnowledgeMap(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicClassificationQuery(studentId, examTypeId, windowSize, limit, from, to);
        var result = await mapHandler.GetKnowledgeMapAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<List<AcademicRecommendationDto>>> GetRecommendations(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] int recommendationLimit = 10,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicRecommendationQuery(studentId, examTypeId, windowSize, limit, recommendationLimit, from, to);
        var result = await recommendationHandler.GetRecommendationsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("study-plan")]
    public async Task<ActionResult<AcademicStudyPlanDto>> GetStudyPlan(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? examTypeId,
        [FromQuery] int windowSize = 3,
        [FromQuery] int? limit = 50,
        [FromQuery] int recommendationLimit = 10,
        [FromQuery] int planDays = 7,
        [FromQuery] int dailyStudyMinutes = 120,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicStudyPlanQuery(
            studentId,
            examTypeId,
            windowSize,
            limit,
            recommendationLimit,
            planDays,
            dailyStudyMinutes,
            startDate,
            from,
            to
        );
        var result = await studyPlanHandler.GetStudyPlanAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("question-styles")]
    public async Task<ActionResult<List<StudentQuestionStyleAnalyticsDto>>> GetQuestionStyleAnalytics(
        [FromRoute] Guid studentId,
        [FromServices] GetStudentQuestionStyleAnalyticsHandler handler,
        [FromQuery] Guid? examTypeId = null,
        [FromQuery] int? window = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicIntelligenceQuery(studentId, examTypeId, window, from, to);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("question-style-trends")]
    public async Task<ActionResult<List<QuestionStyleTrendDto>>> GetQuestionStyleTrends(
        [FromRoute] Guid studentId,
        [FromServices] GetStudentQuestionStyleTrendsHandler handler,
        [FromQuery] Guid? examTypeId = null,
        [FromQuery] int windowSize = 2,
        [FromQuery] int? limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var query = new AcademicTrendQuery(studentId, examTypeId, windowSize, limit, from, to);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }
}



