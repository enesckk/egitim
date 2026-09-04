using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Modules.Students.Features.CreateStudentGoal;
using EgitimPlatform.Modules.Students.Features.DeactivateStudentGoal;
using EgitimPlatform.Modules.Students.Features.GetStudent;
using EgitimPlatform.Modules.Students.Features.GetStudentGoal;
using EgitimPlatform.Modules.Students.Features.GetStudentGoalHistory;
using EgitimPlatform.Modules.Students.Features.ListStudentGoals;
using EgitimPlatform.Modules.Students.Features.ListStudents;
using EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;
using EgitimPlatform.Modules.Students.Features.UpdateStudentGoal;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EgitimPlatform.Modules.Students.Controllers;

[ApiController]
[Route("api/v1/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly CreateStudentHandler _createHandler;
    private readonly GetStudentHandler _getHandler;
    private readonly ListStudentsHandler _listHandler;
    private readonly UpdateAcademicProfileHandler _updateAcademicHandler;
    private readonly CreateStudentGoalHandler _createGoalHandler;
    private readonly UpdateStudentGoalHandler _updateGoalHandler;
    private readonly DeactivateStudentGoalHandler _deactivateGoalHandler;
    private readonly GetStudentGoalHandler _getGoalHandler;
    private readonly ListStudentGoalsHandler _listGoalsHandler;
    private readonly GetStudentGoalHistoryHandler _getGoalHistoryHandler;
    private readonly IValidator<CreateStudentCommand> _createValidator;
    private readonly IValidator<CreateStudentGoalCommand> _createGoalValidator;
    private readonly IValidator<UpdateStudentGoalCommand> _updateGoalValidator;
    private readonly IValidator<UpdateAcademicProfileCommand> _updateAcademicValidator;

    public StudentsController(
        CreateStudentHandler createHandler,
        GetStudentHandler getHandler,
        ListStudentsHandler listHandler,
        UpdateAcademicProfileHandler updateAcademicHandler,
        CreateStudentGoalHandler createGoalHandler,
        UpdateStudentGoalHandler updateGoalHandler,
        DeactivateStudentGoalHandler deactivateGoalHandler,
        GetStudentGoalHandler getGoalHandler,
        ListStudentGoalsHandler listGoalsHandler,
        GetStudentGoalHistoryHandler getGoalHistoryHandler,
        IValidator<CreateStudentCommand> createValidator,
        IValidator<CreateStudentGoalCommand> createGoalValidator,
        IValidator<UpdateStudentGoalCommand> updateGoalValidator,
        IValidator<UpdateAcademicProfileCommand> updateAcademicValidator)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _listHandler = listHandler;
        _updateAcademicHandler = updateAcademicHandler;
        _createGoalHandler = createGoalHandler;
        _updateGoalHandler = updateGoalHandler;
        _deactivateGoalHandler = deactivateGoalHandler;
        _getGoalHandler = getGoalHandler;
        _listGoalsHandler = listGoalsHandler;
        _getGoalHistoryHandler = getGoalHistoryHandler;
        _createValidator = createValidator;
        _createGoalValidator = createGoalValidator;
        _updateGoalValidator = updateGoalValidator;
        _updateAcademicValidator = updateAcademicValidator;
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageStudents)]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateStudentCommand command, CancellationToken ct)
    {
        var validationResult = await _createValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return ValidationProblem(title: "Validation failed", type: "https://tools.ietf.org/html/rfc7231#section-6.5.1", modelStateDictionary: CreateModelState(validationResult));

        var result = await _createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _getHandler.HandleAsync(id, ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(PaginatedList<StudentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] ListStudentsQuery query, CancellationToken ct)
    {
        var result = await _listHandler.HandleAsync(query, ct);
        return Ok(result);
    }

    // Sprint 2 — Academic profile
    [HttpPut("{id:guid}/academic-profile")]
    [Authorize(Policy = Policies.CanManageStudents)]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAcademicProfile(Guid id, [FromBody] UpdateAcademicProfileCommand command, CancellationToken ct)
    {
        if (command.StudentId != id)
            return ValidationProblem(title: "Path/body mismatch", type: "https://tools.ietf.org/html/rfc7231#section-6.5.1");

        var validationResult = await _updateAcademicValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return ValidationProblem(title: "Validation failed", type: "https://tools.ietf.org/html/rfc7231#section-6.5.1", modelStateDictionary: CreateModelState(validationResult));

        var result = await _updateAcademicHandler.HandleAsync(command, ct);
        return Ok(result);
    }

    // Sprint 2 — Student goals
    [HttpPost("{studentId:guid}/goals")]
    [Authorize(Policy = Policies.CanManageStudents)]
    [ProducesResponseType(typeof(StudentGoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGoal(Guid studentId, [FromBody] CreateStudentGoalCommand command, CancellationToken ct)
    {
        if (command.StudentId != studentId)
            return ValidationProblem(title: "Path/body mismatch", type: "https://tools.ietf.org/html/rfc7231#section-6.5.1");

        var validationResult = await _createGoalValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return ValidationProblem(title: "Validation failed", type: "https://tools.ietf.org/html/rfc7231#section-6.5.1", modelStateDictionary: CreateModelState(validationResult));

        var result = await _createGoalHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetGoal), new { goalId = result.Id }, result);
    }

    [HttpGet("{studentId:guid}/goals")]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(IReadOnlyList<StudentGoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListGoals(Guid studentId, [FromQuery] bool? isActive, CancellationToken ct)
    {
        var result = await _listGoalsHandler.HandleAsync(new ListStudentGoalsQuery(studentId, isActive), ct);
        return Ok(result);
    }

    [HttpGet("goals/{goalId:guid}")]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(StudentGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGoal(Guid goalId, CancellationToken ct)
    {
        var result = await _getGoalHandler.HandleAsync(goalId, ct);
        return Ok(result);
    }

    [HttpPut("goals/{goalId:guid}")]
    [Authorize(Policy = Policies.CanManageStudents)]
    [ProducesResponseType(typeof(StudentGoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGoal(Guid goalId, [FromBody] UpdateStudentGoalCommand command, CancellationToken ct)
    {
        if (command.GoalId != goalId)
            return ValidationProblem(title: "Path/body mismatch", type: "https://tools.ietf.org/html/rfc7231#section-6.5.1");

        var validationResult = await _updateGoalValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return ValidationProblem(title: "Validation failed", type: "https://tools.ietf.org/html/rfc7231#section-6.5.1", modelStateDictionary: CreateModelState(validationResult));

        var result = await _updateGoalHandler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpDelete("goals/{goalId:guid}")]
    [Authorize(Policy = Policies.CanManageStudents)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateGoal(Guid goalId, CancellationToken ct)
    {
        await _deactivateGoalHandler.HandleAsync(new DeactivateStudentGoalCommand(goalId), ct);
        return NoContent();
    }

    [HttpGet("goals/{goalId:guid}/history")]
    [Authorize(Policy = Policies.CanViewStudents)]
    [ProducesResponseType(typeof(IReadOnlyList<StudentGoalHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGoalHistory(Guid goalId, CancellationToken ct)
    {
        var result = await _getGoalHistoryHandler.HandleAsync(goalId, ct);
        return Ok(result);
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
