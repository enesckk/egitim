using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
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
    private readonly ICoachStudentQuery _coachStudentQuery;

    public ListStudentsHandler(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICoachStudentQuery coachStudentQuery)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _coachStudentQuery = coachStudentQuery;
    }

    public async Task<PaginatedList<StudentDto>> HandleAsync(ListStudentsQuery query, CancellationToken ct = default)
    {
        var institutionId = await _currentUser.GetInstitutionIdAsync();
        var source = _dbContext.Set<Student>().AsNoTracking();

        // P1-01: Role-based scoping
        if (_currentUser.IsSuperAdmin)
        {
            // SuperAdmin: no filter (privileged scope)
        }
        else if (_currentUser.IsInRole(Roles.Coach))
        {
            // Coach: only students they're actively assigned to
            if (_currentUser.UserId is null) throw new ForbiddenException("User context required.");
            var assignedIds = await _coachStudentQuery.GetActiveAssignedStudentIdsAsync(_currentUser.UserId.Value, ct);
            source = source.Where(s => assignedIds.Contains(s.Id));
        }
        else if (_currentUser.IsInRole(Roles.Student))
        {
            // Student: only own record
            if (_currentUser.UserId is null) throw new ForbiddenException("User context required.");
            var userId = _currentUser.UserId.Value;
            source = source.Where(s => s.UserId == userId);
        }
        else if (_currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            // InstitutionAdmin: own institution
            if (!institutionId.HasValue) throw new ForbiddenException("Institution context required.");
            source = source.Where(s => s.InstitutionId == institutionId.Value);
        }
        else if (_currentUser.IsInRole(Roles.Teacher) || _currentUser.IsInRole(Roles.Parent))
        {
            // Teacher/Parent: no relationship defined yet (Sprint 2) — default deny
            throw new ForbiddenException("Access not yet available for this role.");
        }
        else
        {
            // Unknown role or no institution → fail closed
            throw new ForbiddenException("Access denied.");
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
