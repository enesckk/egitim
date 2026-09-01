using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Modules.Students.Features.GetStudent;
using EgitimPlatform.Modules.Students.Features.ListStudents;
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
    private readonly IValidator<CreateStudentCommand> _createValidator;

    public StudentsController(
        CreateStudentHandler createHandler,
        GetStudentHandler getHandler,
        ListStudentsHandler listHandler,
        IValidator<CreateStudentCommand> createValidator)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _listHandler = listHandler;
        _createValidator = createValidator;
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
        {
            return ValidationProblem(
                title: "Validation failed",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                modelStateDictionary: CreateModelState(validationResult));
        }

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
