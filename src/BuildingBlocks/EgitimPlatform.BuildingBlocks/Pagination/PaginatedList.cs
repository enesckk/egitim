using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.BuildingBlocks.Pagination;

public sealed class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PaginatedList(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, page, pageSize);
    }
}
