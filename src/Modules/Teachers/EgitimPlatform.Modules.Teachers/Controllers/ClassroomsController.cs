using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Teachers.Features.ManageClassrooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Teachers.Controllers;

[ApiController]
[Route("api/v1/classrooms")]
[Authorize]
public class ClassroomsController(ClassroomHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClassroomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ClassroomDto>>> List(
        [FromQuery] ListClassroomsQuery query,
        CancellationToken ct)
    {
        var result = await handler.ListAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassroomDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await handler.GetAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageTeachers)]
    [ProducesResponseType(typeof(ClassroomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassroomDto>> Create([FromBody] CreateClassroomCommand command, CancellationToken ct)
    {
        var result = await handler.CreateAsync(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/teachers")]
    [Authorize(Policy = Policies.CanManageTeachers)]
    [ProducesResponseType(typeof(ClassroomTeacherDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClassroomTeacherDto>> AssignTeacher(
        Guid id,
        [FromBody] AssignClassroomTeacherCommand command,
        CancellationToken ct)
    {
        if (id != command.ClassroomId)
            return BadRequest("ClassroomId path and body mismatch.");

        var result = await handler.AssignTeacherAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{id:guid}/teachers/{teacherId:guid}")]
    [Authorize(Policy = Policies.CanManageTeachers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveTeacher(Guid id, Guid teacherId, CancellationToken ct)
    {
        await handler.RemoveTeacherAsync(id, teacherId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/students")]
    [Authorize(Policy = Policies.CanManageTeachers)]
    [ProducesResponseType(typeof(ClassroomStudentItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClassroomStudentItemDto>> AssignStudent(
        Guid id,
        [FromBody] AssignClassroomStudentCommand command,
        CancellationToken ct)
    {
        if (id != command.ClassroomId)
            return BadRequest("ClassroomId path and body mismatch.");

        var result = await handler.AssignStudentAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{id:guid}/students/{studentId:guid}")]
    [Authorize(Policy = Policies.CanManageTeachers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveStudent(Guid id, Guid studentId, CancellationToken ct)
    {
        await handler.RemoveStudentAsync(id, studentId, ct);
        return NoContent();
    }
}
