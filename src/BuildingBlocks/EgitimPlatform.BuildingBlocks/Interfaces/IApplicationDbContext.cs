using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.BuildingBlocks.Interfaces;

/// <summary>
/// Abstraction over the shared DbContext.
/// P2-12: Domain modules depend on this interface (from BuildingBlocks),
/// NOT on the concrete ApplicationDbContext (owned by Infrastructure).
/// This decouples domain modules from the Identity/Infrastructure persistence layer.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
