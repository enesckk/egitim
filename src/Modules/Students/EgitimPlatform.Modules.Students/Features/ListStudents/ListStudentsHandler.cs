using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.ListStudents;

public class ListStudentsHandler
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListStudentsHandler(ApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<StudentDto>> HandleAsync(ListStudentsQuery query, CancellationToken ct = default)
    {
        var institutionId = await _currentUser.GetInstitutionIdAsync();

        var source = _dbContext.Set<Student>().AsNoTracking();

        // Institution filter (defense-in-depth; authorization should already ensure this)
        if (!_currentUser.IsSuperAdmin && institutionId.HasValue)
        {
            source = source.Where(s => s.InstitutionId == institutionId.Value);
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            source = source.Where(s =>
                s.FirstName.Contains(term) ||
                s.LastName.Contains(term));
        }

        // Order by name
        source = source.OrderBy(s => s.LastName).ThenBy(s => s.FirstName);

        // Project to DTO
        var dtoSource = source.Select(s => new StudentDto(
            s.Id,
            s.FirstName,
            s.LastName,
            s.FirstName + " " + s.LastName,
            s.Status.ToString(),
            s.InstitutionId,
            s.CreatedAt));

        return await PaginatedList<StudentDto>.CreateAsync(dtoSource, query.Page, query.PageSize, ct);
    }
}
