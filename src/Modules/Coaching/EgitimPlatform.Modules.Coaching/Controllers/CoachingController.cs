using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using EgitimPlatform.Modules.Coaching.Features.ManageCoaches;
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
