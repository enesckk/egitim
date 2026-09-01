namespace EgitimPlatform.BuildingBlocks.Interfaces;

/// <summary>
/// Marker interface for entities that support soft deletion.
/// Used by ApplicationDbContext to apply global query filters automatically.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
