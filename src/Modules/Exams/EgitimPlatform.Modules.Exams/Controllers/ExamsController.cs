using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Exams.Features.CreateExam;
using EgitimPlatform.Modules.Exams.Features.GetExamDetail;
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
    public async Task<ActionResult<PaginatedList<ExamDto>>> ListExams(
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
}
