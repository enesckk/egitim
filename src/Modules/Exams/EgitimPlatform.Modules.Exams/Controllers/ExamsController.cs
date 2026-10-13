using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Exams.Features.AddExamQuestion;
using EgitimPlatform.Modules.Exams.Features.CreateExam;
using EgitimPlatform.Modules.Exams.Features.GetExamDetail;
using EgitimPlatform.Modules.Exams.Features.ListExamQuestions;
using EgitimPlatform.Modules.Exams.Features.ListExams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Exams.Controllers;

[ApiController]
[Route("api/v1/exams")]
public class ExamsController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.CanManageExams)]
    public async Task<ActionResult<Guid>> CreateExam(
        [FromBody] CreateExamCommand command,
        [FromServices] CreateExamHandler handler,
        CancellationToken ct)
    {
        var examId = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetExamDetail), new { examId }, examId);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<EgitimPlatform.BuildingBlocks.Pagination.PaginatedList<ExamDto>>> ListExams(
        [FromQuery] ListExamsQuery query,
        [FromServices] ListExamsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{examId:guid}")]
    [Authorize]
    public async Task<ActionResult<ExamDetailDto>> GetExamDetail(
        [FromRoute] Guid examId,
        [FromServices] GetExamDetailHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(examId, ct);
        return Ok(result);
    }

    [HttpPost("{examId:guid}/questions")]
    [Authorize(Policy = Policies.CanManageExams)]
    public async Task<ActionResult<Guid>> AddExamQuestion(
        [FromRoute] Guid examId,
        [FromBody] AddExamQuestionCommand command,
        [FromServices] AddExamQuestionHandler handler,
        CancellationToken ct)
    {
        var id = await handler.HandleAsync(examId, command, ct);
        return Ok(id);
    }

    [HttpGet("{examId:guid}/questions")]
    [Authorize]
    public async Task<ActionResult<object>> ListExamQuestions(
        [FromRoute] Guid examId,
        [FromServices] ListExamQuestionsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(examId, forceStudentView: false, ct);
        return Ok(result);
    }
}
