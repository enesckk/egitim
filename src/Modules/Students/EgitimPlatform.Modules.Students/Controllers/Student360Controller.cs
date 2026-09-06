using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Students.Features.GetStudent360;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EgitimPlatform.Modules.Students.Controllers;
[ApiController, Route("api/v1/students"), Authorize(Policy = Policies.CanViewStudents)]
public class Student360Controller(GetStudent360Handler handler) : ControllerBase
{
    [HttpGet("{studentId:guid}/360")]
    public async Task<ActionResult<Student360Dto>> Get(Guid studentId, CancellationToken ct) => Ok(await handler.HandleAsync(new(studentId), ct));
}
