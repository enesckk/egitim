using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Content.Features.ManageMaterials;
using EgitimPlatform.Modules.Content.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EgitimPlatform.Modules.Content.Controllers;

[ApiController]
[Route("api/content/materials")]
[Authorize]
public class ContentMaterialsController : ControllerBase
{
    private readonly IMaterialsHandler _handler;

    public ContentMaterialsController(IMaterialsHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("teacher")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> GetTeacherMaterials(CancellationToken cancellationToken)
    {
        var result = await _handler.GetTeacherMaterialsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Teacher)]
    [EnableRateLimiting("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LocalFileStorageService.MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = LocalFileStorageService.MaxFileSize)]
    public async Task<IActionResult> CreateMaterial([FromForm] CreateMaterialFormModel form, CancellationToken cancellationToken)
    {
        List<Guid>? classIds = null;
        if (!string.IsNullOrWhiteSpace(form.AssignClassroomIds))
        {
            classIds = form.AssignClassroomIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Guid.Parse)
                .ToList();
        }

        var request = new CreateMaterialRequest(
            form.Title,
            form.Subject,
            form.Topic,
            form.Grade,
            form.MaterialType ?? "fasikul",
            form.Description,
            form.PageCount,
            form.QuestionCount,
            form.DurationMinutes,
            form.File,
            classIds
        );

        var result = await _handler.CreateMaterialAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{materialId:guid}/assign")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> AssignToClassroom(Guid materialId, [FromBody] AssignMaterialRequest request, CancellationToken cancellationToken)
    {
        var result = await _handler.AssignToClassroomAsync(materialId, request, cancellationToken);
        return Ok(new { success = result });
    }

    [HttpPost("{materialId:guid}/revoke/{classroomId:guid}")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> RevokeAssignment(Guid materialId, Guid classroomId, CancellationToken cancellationToken)
    {
        var result = await _handler.RevokeAssignmentAsync(materialId, classroomId, cancellationToken);
        return Ok(new { success = result });
    }

    [HttpGet("student")]
    [Authorize(Roles = Roles.Student)]
    public async Task<IActionResult> GetStudentMaterials(CancellationToken cancellationToken)
    {
        var result = await _handler.GetStudentMaterialsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{materialId:guid}/download")]
    public async Task<IActionResult> DownloadMaterial(Guid materialId, CancellationToken cancellationToken)
    {
        var (stream, contentType, fileName) = await _handler.DownloadMaterialAsync(materialId, cancellationToken);
        return File(stream, contentType, fileName);
    }
}

public class CreateMaterialFormModel
{
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public string? Grade { get; set; }
    public string? MaterialType { get; set; }
    public string? Description { get; set; }
    public int? PageCount { get; set; }
    public int? QuestionCount { get; set; }
    public int? DurationMinutes { get; set; }
    public IFormFile File { get; set; } = null!;
    public string? AssignClassroomIds { get; set; } // comma separated Guids
}
