using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using EgitimPlatform.Modules.Coaching.Features.ManageCoaches;
using EgitimPlatform.Modules.Coaching.Features.ManageNotes;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EgitimPlatform.Modules.Coaching.Controllers;

[ApiController]
[Route("api/v1/coaching")]
[Authorize]
public class CoachingController : ControllerBase
{
    private readonly AssignCoachHandler _assignHandler;
    private readonly IValidator<AssignCoachCommand> _assignValidator;
    private readonly CoachManagementHandler? _coachManagementHandler;
    private readonly EgitimPlatform.BuildingBlocks.Interfaces.ICurrentUser _currentUser;

    public CoachingController(
        AssignCoachHandler assignHandler,
        IValidator<AssignCoachCommand> assignValidator,
        EgitimPlatform.BuildingBlocks.Interfaces.ICurrentUser currentUser,
        CoachManagementHandler? coachManagementHandler = null)
    {
        _assignHandler = assignHandler;
        _assignValidator = assignValidator;
        _currentUser = currentUser;
        _coachManagementHandler = coachManagementHandler;
    }

    [HttpGet("coaches")]
    [Authorize(Policy = Policies.CanAssignCoach)]
    [ProducesResponseType(typeof(IReadOnlyList<CoachSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoachSummaryDto>>> ListCoaches(
        [FromQuery] ListCoachesQuery query,
        CancellationToken ct)
    {
        if (_coachManagementHandler is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var result = await _coachManagementHandler.ListAsync(query, ct);
        return Ok(result);
    }

    [HttpPost("coaches")]
    [Authorize(Policy = Policies.CanAssignCoach)]
    [ProducesResponseType(typeof(CoachSummaryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CoachSummaryDto>> CreateCoach(
        [FromBody] CreateCoachCommand command,
        CancellationToken ct)
    {
        if (_coachManagementHandler is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        if (command.InstitutionId == Guid.Empty)
        {
            var userInstId = await _currentUser.GetInstitutionIdAsync();
            if (userInstId.HasValue)
            {
                command = command with { InstitutionId = userInstId.Value };
            }
        }

        var result = await _coachManagementHandler.CreateAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("assign")]
    [Authorize(Policy = Policies.CanAssignCoach)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignCoach([FromBody] AssignCoachCommand command, CancellationToken ct)
    {
        var validationResult = await _assignValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(
                title: "Validation failed",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                modelStateDictionary: CreateModelState(validationResult));
        }

        await _assignHandler.HandleAsync(command, ct);
        return NoContent();
    }

    [HttpGet("students/{studentId:guid}/notes")]
    [Authorize(Policy = Policies.CanManageCoachPrivateNotes)]
    [ProducesResponseType(typeof(IReadOnlyList<CoachPrivateNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CoachPrivateNoteDto>>> ListNotes(
        [FromRoute] Guid studentId,
        [FromServices] CoachPrivateNotesHandler notesHandler,
        CancellationToken ct)
    {
        var result = await notesHandler.ListAsync(new(studentId), ct);
        return Ok(result);
    }

    [HttpPost("students/{studentId:guid}/notes")]
    [Authorize(Policy = Policies.CanManageCoachPrivateNotes)]
    [ProducesResponseType(typeof(CoachPrivateNoteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CoachPrivateNoteDto>> CreateNote(
        [FromRoute] Guid studentId,
        [FromBody] CreateCoachPrivateNoteRequest request,
        [FromServices] CoachPrivateNotesHandler notesHandler,
        CancellationToken ct)
    {
        var command = new CreateCoachPrivateNoteCommand(studentId, request.Category, request.Content);
        var result = await notesHandler.CreateAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("notes/{noteId:guid}")]
    [Authorize(Policy = Policies.CanManageCoachPrivateNotes)]
    [ProducesResponseType(typeof(CoachPrivateNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoachPrivateNoteDto>> UpdateNote(
        [FromRoute] Guid noteId,
        [FromBody] UpdateCoachPrivateNoteRequest request,
        [FromServices] CoachPrivateNotesHandler notesHandler,
        CancellationToken ct)
    {
        var command = new UpdateCoachPrivateNoteCommand(noteId, request.Category, request.Content);
        var result = await notesHandler.UpdateAsync(command, ct);
        return Ok(result);
    }

    [HttpDelete("notes/{noteId:guid}")]
    [Authorize(Policy = Policies.CanManageCoachPrivateNotes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteNote(
        [FromRoute] Guid noteId,
        [FromServices] CoachPrivateNotesHandler notesHandler,
        CancellationToken ct)
    {
        await notesHandler.DeleteAsync(noteId, ct);
        return NoContent();
    }

    [HttpGet("meetings")]
    [Authorize(Policy = Policies.CanManageCoachPrivateNotes)]
    [ProducesResponseType(typeof(IReadOnlyList<EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingDto>>> ListMeetings(
        [FromServices] EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingsHandler meetingsHandler,
        CancellationToken ct)
    {
        var result = await meetingsHandler.ListAsync(ct);
        return Ok(result);
    }

    [HttpPost("meetings")]
    [Authorize(Policy = Policies.CanManageCoachPrivateNotes)]
    [ProducesResponseType(typeof(EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingDto>> CreateMeeting(
        [FromBody] EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CreateCoachMeetingRequest request,
        [FromServices] EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingsHandler meetingsHandler,
        CancellationToken ct)
    {
        var result = await meetingsHandler.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("meetings/{meetingId:guid}")]
    [Authorize(Policy = Policies.CanManageCoachPrivateNotes)]
    [ProducesResponseType(typeof(EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingDto>> UpdateMeeting(
        [FromRoute] Guid meetingId,
        [FromBody] EgitimPlatform.Modules.Coaching.Features.ManageMeetings.UpdateCoachMeetingRequest request,
        [FromServices] EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingsHandler meetingsHandler,
        CancellationToken ct)
    {
        var result = await meetingsHandler.UpdateAsync(meetingId, request, ct);
        return Ok(result);
    }

    [HttpDelete("meetings/{meetingId:guid}")]
    [Authorize(Policy = Policies.CanManageCoachPrivateNotes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMeeting(
        [FromRoute] Guid meetingId,
        [FromServices] EgitimPlatform.Modules.Coaching.Features.ManageMeetings.CoachMeetingsHandler meetingsHandler,
        CancellationToken ct)
    {
        await meetingsHandler.DeleteAsync(meetingId, ct);
        return NoContent();
    }

    private static ModelStateDictionary CreateModelState(FluentValidation.Results.ValidationResult validationResult)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in validationResult.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
        return modelState;
    }
}
