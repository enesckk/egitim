using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Exams.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EgitimPlatform.Modules.Exams.Controllers;

[ApiController]
[Route("api/v1/exam-attempts")]
[Authorize(Policy = Policies.CanRecordExamResults)]
public sealed class ExamAttemptsController(ExamAttemptHandler handler,
    IValidator<UpdateExamAttemptCommand> updateValidator,
    IValidator<FinalizeExamAttemptCommand> finalizeValidator) : ControllerBase
{
    [HttpPut("{attemptId:guid}")]
    public async Task<IActionResult> Update(Guid attemptId, UpdateExamAttemptCommand command, CancellationToken ct)
    {
        var invalid = await ValidateAsync(updateValidator, command, ct); if (invalid is not null) return invalid;
        return Ok(await handler.UpdateAsync(attemptId, command, ct));
    }

    [HttpPost("{attemptId:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid attemptId, FinalizeExamAttemptCommand command, CancellationToken ct)
    {
        var invalid = await ValidateAsync(finalizeValidator, command, ct); if (invalid is not null) return invalid;
        return Ok(await handler.FinalizeAsync(attemptId, command, ct));
    }

    private async Task<IActionResult?> ValidateAsync<T>(IValidator<T> validator, T model, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(model, ct);
        if (result.IsValid) return null;
        var state = new ModelStateDictionary();
        foreach (var error in result.Errors) state.AddModelError(error.PropertyName, error.ErrorMessage);
        return ValidationProblem(title: "Validation failed", modelStateDictionary: state);
    }
}
