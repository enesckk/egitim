using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EgitimPlatform.Modules.Teachers.Controllers;
[ApiController, Route("api/v1/teachers"), Authorize]
public class TeachersController(TeacherHandler handler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.CanManageTeachers)]
    public async Task<ActionResult<IReadOnlyList<TeacherListDto>>> List([FromQuery] ListTeachersQuery query, CancellationToken ct)
        => Ok(await handler.ListAsync(query, ct));
    [HttpGet("{teacherId:guid}")]
    [Authorize(Policy = Policies.CanManageTeachers)]
    public async Task<ActionResult<TeacherDetailDto>> Get(Guid teacherId, CancellationToken ct)
        => Ok(await handler.GetAsync(teacherId, ct));
    [HttpGet("{teacherId:guid}/subjects")]
    public async Task<ActionResult<IReadOnlyList<TeacherSubjectDto>>> Subjects(Guid teacherId, CancellationToken ct) => Ok(await handler.GetSubjectsAsync(teacherId, ct));
    [Authorize(Policy = Policies.CanManageTeachers)]
    [HttpPost]
    public async Task<ActionResult<TeacherDto>> Create(CreateTeacherCommand c, CancellationToken ct) => StatusCode(201, await handler.CreateAsync(c, ct));
    [Authorize(Policy = Policies.CanManageTeachers)]
    [HttpPost("{teacherId:guid}/subjects/{subjectId:guid}")]
    public async Task<ActionResult<TeacherSubjectDto>> Assign(Guid teacherId, Guid subjectId, CancellationToken ct) => StatusCode(201, await handler.SetSubjectAsync(new(teacherId, subjectId), false, ct));
    [Authorize(Policy = Policies.CanManageTeachers)]
    [HttpDelete("{teacherId:guid}/subjects/{subjectId:guid}")]
    public async Task<IActionResult> Remove(Guid teacherId, Guid subjectId, CancellationToken ct)
    {
        await handler.SetSubjectAsync(new(teacherId, subjectId), true, ct); return NoContent();
    }
}
