using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;
namespace EgitimPlatform.Modules.Academic.Entities;
/// <summary>Shared reference data. Codes and hierarchy are stable; no institution ownership.</summary>
public class Concept : SoftDeletableEntity, IStableReference
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid LearningOutcomeId { get; set; }
}
