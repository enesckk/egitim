using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Coaching.Entities;

public class CoachPrivateNote : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid CoachId { get; set; }
    public Guid StudentId { get; set; }
    public string Category { get; set; } = "Genel";
    public string Content { get; set; } = string.Empty;
}
