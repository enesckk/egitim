using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Coaching.Controllers;

[ApiController]
[Route("api/v1/coaching")]
[Authorize]
public class CoachingController : ControllerBase
{
    private readonly AssignCoachHandler _assignHandler;
    private readonly IValidator<AssignCoachCommand> _assignValidator;

    public CoachingController(
        AssignCoachHandler assignHandler,
        IValidator<AssignCoachCommand> assignValidator)
    {
        _assignHandler = assignHandler;
        _assignValidator = assignValidator;
    }

    [HttpPost("assign")]
    [Authorize(Policy = Policies.CanAssignCoach)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignCoach([FromBody] AssignCoachCommand command, CancellationToken ct)
    {
        var validationResult = await _assignValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        await _assignHandler.HandleAsync(command, ct);
        return NoContent();
    }
}
