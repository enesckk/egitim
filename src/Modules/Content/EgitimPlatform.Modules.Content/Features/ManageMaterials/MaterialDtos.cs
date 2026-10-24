using Microsoft.AspNetCore.Http;

namespace EgitimPlatform.Modules.Content.Features.ManageMaterials;

public record ContentMaterialItemDto(
    Guid Id,
    string Title,
    string Subject,
    string Topic,
    string Grade,
    string MaterialType,
    string? Description,
    int? PageCount,
    int? QuestionCount,
    int? DurationMinutes,
    string OriginalFileName,
    long FileSizeBytes,
    int AssignedClassesCount,
    DateTimeOffset CreatedAt,
    List<ContentMaterialAssignmentDto> Assignments
);

public record ContentMaterialAssignmentDto(
    Guid Id,
    Guid ClassroomId,
    string? ClassroomName,
    string? Note,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record CreateMaterialRequest(
    string Title,
    string Subject,
    string? Topic,
    string? Grade,
    string? MaterialType,
    string? Description,
    int? PageCount,
    int? QuestionCount,
    int? DurationMinutes,
    IFormFile File,
    List<Guid>? AssignClassroomIds
);

public record AssignMaterialRequest(
    Guid ClassroomId,
    string? Note
);

public record StudentMaterialDto(
    Guid Id,
    string Title,
    string Subject,
    string Topic,
    string Grade,
    string MaterialType,
    string? Description,
    int? PageCount,
    int? QuestionCount,
    int? DurationMinutes,
    string OriginalFileName,
    long FileSizeBytes,
    string ClassroomName,
    string? TeacherNote,
    DateTimeOffset AssignedAt
);
