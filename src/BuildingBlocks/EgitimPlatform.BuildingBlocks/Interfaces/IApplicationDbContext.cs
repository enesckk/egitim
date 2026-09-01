namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
