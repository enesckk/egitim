using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Content.Entities;

public class ContentMaterial : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid TeacherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string MaterialType { get; set; } = "fasikul"; // fasikul, test, soru_bankasi, deneme
    public string? Description { get; set; }

    public int? PageCount { get; set; }
    public int? QuestionCount { get; set; }
    public int? DurationMinutes { get; set; }

    // File storage details
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; }

    public ICollection<ContentMaterialAssignment> Assignments { get; set; } = new List<ContentMaterialAssignment>();
}
