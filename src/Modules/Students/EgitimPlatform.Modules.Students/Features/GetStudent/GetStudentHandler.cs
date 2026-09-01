using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.GetStudent;

public class GetStudentHandler
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public GetStudentHandler(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICoachStudentQuery coachStudentQuery)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _coachStudentQuery = coachStudentQuery;
    }

    public async Task<StudentDto> HandleAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await _dbContext.Set<Student>()
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        // Return 404 uniformly (no existence leak) for not found OR access denied
        if (student is null)
            throw new NotFoundException("Student", studentId);

        // P1-01: Role-based authorization
        if (_currentUser.IsSuperAdmin)
        {
            // SuperAdmin: privileged access
        }
        else if (_currentUser.IsInRole(Roles.Coach))
        {
            // Coach: must have active assignment to this student
            if (_currentUser.UserId is null) throw new ForbiddenException("Access denied.");
            var hasAssignment = await _coachStudentQuery.HasActiveAssignmentAsync(_currentUser.UserId.Value, studentId, ct);
            if (!hasAssignment) throw new ForbiddenException("Access denied.");

            // Same-institution check (defense-in-depth)
            var institutionId = await _currentUser.GetInstitutionIdAsync();
            if (institutionId.HasValue && student.InstitutionId != institutionId.Value)
                throw new ForbiddenException("Access denied.");
        }
        else if (_currentUser.IsInRole(Roles.Student))
        {
            // Student: only own record
            if (_currentUser.UserId is null || student.UserId != _currentUser.UserId.Value)
                throw new ForbiddenException("Access denied.");
        }
        else if (_currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            // InstitutionAdmin: own institution
            var institutionId = await _currentUser.GetInstitutionIdAsync();
            if (!institutionId.HasValue || student.InstitutionId != institutionId.Value)
                throw new ForbiddenException("Access denied.");
        }
        else if (_currentUser.IsInRole(Roles.Teacher) || _currentUser.IsInRole(Roles.Parent))
        {
            // Teacher/Parent: no relationship defined yet (Sprint 2) — default deny
            throw new ForbiddenException("Access not yet available for this role.");
        }
        else
        {
            throw new ForbiddenException("Access denied.");
        }

        return student.ToDto();
    }
}
