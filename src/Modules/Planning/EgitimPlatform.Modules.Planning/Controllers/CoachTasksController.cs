using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Planning.Controllers;

[ApiController]
[Route("api/v1/students/{studentId:guid}/coach-tasks")]
[Authorize]
public class CoachTasksController(
    CreateCoachTaskHandler createHandler,
    ListCoachTasksHandler listHandler,
    UpdateCoachTaskHandler updateHandler,
    CancelCoachTaskHandler cancelHandler,
    UpdateCoachTaskCompletionHandler completionHandler) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(CoachTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CoachTaskDto>> Create(
        [FromRoute] Guid studentId,
        [FromBody] CreateCoachTaskCommand command,
        CancellationToken ct)
    {
        var result = await createHandler.HandleAsync(studentId, command, ct);
        return CreatedAtAction(nameof(List), new { studentId }, result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(List<CoachTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CoachTaskDto>>> List(
        [FromRoute] Guid studentId,
        [FromQuery] CoachTaskStatus? status,
        CancellationToken ct)
    {
        var result = await listHandler.HandleAsync(studentId, status, ct);
        return Ok(result);
    }

    [HttpPut("{taskId:guid}")]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(CoachTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CoachTaskDto>> Update(
        [FromRoute] Guid studentId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateCoachTaskCommand command,
        CancellationToken ct)
    {
        var result = await updateHandler.HandleAsync(studentId, taskId, command, ct);
        return Ok(result);
    }

    [HttpPost("{taskId:guid}/cancel")]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(CoachTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoachTaskDto>> Cancel(
        [FromRoute] Guid studentId,
        [FromRoute] Guid taskId,
        CancellationToken ct)
    {
        var result = await cancelHandler.HandleAsync(studentId, taskId, ct);
        return Ok(result);
    }

    [HttpPatch("{taskId:guid}/completion")]
    [Authorize(Policy = Policies.CanEditOwnStudent)]
    [ProducesResponseType(typeof(CoachTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoachTaskDto>> UpdateCompletion(
        [FromRoute] Guid studentId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateCoachTaskCompletionCommand command,
        CancellationToken ct)
    {
        var result = await completionHandler.HandleAsync(studentId, taskId, command, ct);
        return Ok(result);
    }
}
