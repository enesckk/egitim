using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EgitimPlatform.Modules.Teachers.Controllers;
[ApiController, Route("api/v1/teachers"), Authorize]
public class TeachersController(TeacherHandler handler) : ControllerBase
{
    [HttpGet("{teacherId:guid}/subjects")]
    public async Task<ActionResult<IReadOnlyList<TeacherSubjectDto>>> Subjects(Guid teacherId, CancellationToken ct) => Ok(await handler.GetSubjectsAsync(teacherId, ct));
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.InstitutionAdmin)]
    [HttpPost]
    public async Task<ActionResult<TeacherDto>> Create(CreateTeacherCommand c, CancellationToken ct) => StatusCode(201, await handler.CreateAsync(c, ct));
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.InstitutionAdmin)]
    [HttpPost("{teacherId:guid}/subjects/{subjectId:guid}")]
    public async Task<ActionResult<TeacherSubjectDto>> Assign(Guid teacherId, Guid subjectId, CancellationToken ct) => StatusCode(201, await handler.SetSubjectAsync(new(teacherId, subjectId), false, ct));
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.InstitutionAdmin)]
    [HttpDelete("{teacherId:guid}/subjects/{subjectId:guid}")]
    public async Task<IActionResult> Remove(Guid teacherId, Guid subjectId, CancellationToken ct)
    {
        await handler.SetSubjectAsync(new(teacherId, subjectId), true, ct); return NoContent();
    }
}
