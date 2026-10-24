using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Content.Entities;

public class ContentMaterialAssignment : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid ClassroomId { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;

    public ContentMaterial? Material { get; set; }
}
