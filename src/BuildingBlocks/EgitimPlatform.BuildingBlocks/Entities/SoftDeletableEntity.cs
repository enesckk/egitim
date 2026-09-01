using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.BuildingBlocks.Entities;

public abstract class SoftDeletableEntity : AuditableEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
