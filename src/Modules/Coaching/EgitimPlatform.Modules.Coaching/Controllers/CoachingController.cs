using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
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
