using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Planning.Controllers;

[ApiController]
[Route("api/v1/students/{studentId:guid}/study-plans")]
[Authorize]
public class StudyPlansController(
    AcceptAcademicStudyPlanHandler acceptHandler,
    GetActiveStudyPlanHandler getActiveHandler,
    GetStudyPlanHandler getHandler,
    ListStudyPlansHandler listHandler,
    UpdateTaskCompletionHandler updateCompletionHandler,
    CancelStudyPlanHandler cancelHandler) : ControllerBase
{
    [HttpPost("from-academic-intelligence")]
    [Authorize(Policy = Policies.CanEditOwnStudent)]
    [ProducesResponseType(typeof(StudyPlanDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudyPlanDetailDto>> AcceptFromAcademicIntelligence(
        [FromRoute] Guid studentId,
        [FromBody] AcceptAcademicStudyPlanCommand command,
        CancellationToken ct)
    {
        if (command.StudentId != studentId)
        {
            return BadRequest("StudentId in path must match command body.");
        }

        var result = await acceptHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { studentId = result.Plan.StudentId, planId = result.Plan.Id }, result);
    }

    [HttpGet("active")]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(StudyPlanDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyPlanDetailDto>> GetActive(
        [FromRoute] Guid studentId,
        CancellationToken ct)
    {
        var result = await getActiveHandler.HandleAsync(studentId, ct);
        return Ok(result);
    }

    [HttpGet("{planId:guid}")]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(StudyPlanDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyPlanDetailDto>> GetById(
        [FromRoute] Guid studentId,
        [FromRoute] Guid planId,
        CancellationToken ct)
    {
        var result = await getHandler.HandleAsync(studentId, planId, ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(List<StudyPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StudyPlanDto>>> List(
        [FromRoute] Guid studentId,
        CancellationToken ct)
    {
        var result = await listHandler.HandleAsync(studentId, ct);
        return Ok(result);
    }

    [HttpPatch("{planId:guid}/tasks/{taskId:guid}/completion")]
    [Authorize(Policy = Policies.CanEditOwnStudent)]
    [ProducesResponseType(typeof(StudyTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyTaskDto>> UpdateTaskCompletion(
        [FromRoute] Guid studentId,
        [FromRoute] Guid planId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskCompletionCommand command,
        CancellationToken ct)
    {
        var result = await updateCompletionHandler.HandleAsync(studentId, planId, taskId, command, ct);
        return Ok(result);
    }

    [HttpPost("{planId:guid}/cancel")]
    [Authorize(Policy = Policies.CanEditOwnStudent)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid studentId,
        [FromRoute] Guid planId,
        CancellationToken ct)
    {
        await cancelHandler.HandleAsync(studentId, planId, ct);
        return NoContent();
    }
}
