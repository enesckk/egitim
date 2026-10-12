using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Exams.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EgitimPlatform.Modules.Exams.Controllers;

[ApiController]
[Route("api/v1/exams")]
[Authorize]
public sealed class ExamsController(ExamHandler exams, ExamAttemptHandler attempts,
    IValidator<CreateExamCommand> createValidator, IValidator<ListExamsQuery> listValidator,
    IValidator<CreateExamAttemptCommand> attemptValidator,
    IValidator<PublishExamCommand> publishValidator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.CanManageExams)]
    [ProducesResponseType(typeof(ExamDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateExamCommand command, CancellationToken ct)
    {
        var invalid = await ValidateAsync(createValidator, command, ct); if (invalid is not null) return invalid;
        var result = await exams.CreateAsync(command, ct);
        return CreatedAtAction(nameof(Get), new { examId = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanManageExams)]
    [ProducesResponseType(typeof(PaginatedList<ExamListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] ListExamsQuery query, CancellationToken ct)
    {
        var invalid = await ValidateAsync(listValidator, query, ct); if (invalid is not null) return invalid;
        return Ok(await exams.ListAsync(query, ct));
    }

    [HttpGet("{examId:guid}")]
    [Authorize(Policy = Policies.CanManageExams)]
    public async Task<IActionResult> Get(Guid examId, CancellationToken ct) => Ok(await exams.GetAsync(examId, ct));

    [HttpPost("{examId:guid}/publish")]
    [Authorize(Policy = Policies.CanManageExams)]
    [ProducesResponseType(typeof(ExamDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid examId, PublishExamCommand command, CancellationToken ct)
    {
        var invalid = await ValidateAsync(publishValidator, command, ct); if (invalid is not null) return invalid;
        return Ok(await exams.PublishAsync(examId, command, ct));
    }

    [HttpPost("{examId:guid}/attempts")]
    [Authorize(Policy = Policies.CanRecordExamResults)]
    [ProducesResponseType(typeof(ExamAttemptMutationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAttempt(Guid examId, CreateExamAttemptCommand command, CancellationToken ct)
    {
        var invalid = await ValidateAsync(attemptValidator, command, ct); if (invalid is not null) return invalid;
        var result = await attempts.CreateAsync(examId, command, ct);
        return Created($"/api/v1/students/{result.StudentId}/exam-results/{result.Id}", result);
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
