using EgitimPlatform.BuildingBlocks.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.BuildingBlocks.Extensions;

public static class QueryExtensions
{
    public static IQueryable<T> WithInstitution<T>(
        this IQueryable<T> query,
        Guid institutionId)
        where T : class, IHasInstitutionId
    {
        return query.Where(e => e.InstitutionId == institutionId);
    }

    public static async Task<bool> AnyWithInstitutionAsync<T>(
        this IQueryable<T> query,
        Guid institutionId,
        CancellationToken cancellationToken = default)
        where T : class, IHasInstitutionId
    {
        return await query.AnyAsync(e => e.InstitutionId == institutionId, cancellationToken);
    }
}
