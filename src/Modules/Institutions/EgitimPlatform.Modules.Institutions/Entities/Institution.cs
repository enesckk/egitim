using EgitimPlatform.BuildingBlocks.Entities;

namespace EgitimPlatform.Modules.Institutions.Entities;

public class Institution : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
