using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Exams.Features.CreateManualResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Exams.Controllers;

[ApiController]
[Route("api/v1/exams/{examId:guid}/manual-results")]
public class ManualResultsController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    public async Task<ActionResult<SingleManualResultResponseDto>> CreateSingleManualResult(
        [FromRoute] Guid examId,
        [FromBody] CreateSingleManualResultCommand command,
        [FromServices] CreateSingleManualResultHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(examId, command, ct);
        return Ok(result);
    }

    [HttpPost("bulk")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    public async Task<ActionResult<BulkManualResultResponseDto>> CreateBulkManualResult(
        [FromRoute] Guid examId,
        [FromBody] CreateBulkManualResultCommand command,
        [FromServices] CreateBulkManualResultHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(examId, command, ct);
        return Ok(result);
    }
}
