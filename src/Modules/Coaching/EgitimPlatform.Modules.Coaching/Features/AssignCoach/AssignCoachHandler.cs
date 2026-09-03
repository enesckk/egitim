using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;

using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Coaching.Features.AssignCoach;

public class AssignCoachHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public AssignCoachHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task HandleAsync(AssignCoachCommand command, CancellationToken ct = default)
    {
        // P1-02: Only SuperAdmin and InstitutionAdmin can use this endpoint.
        // Coach is not allowed — they get auto-assignment when creating students.
        if (!_currentUser.IsSuperAdmin && !_currentUser.IsInRole(Roles.InstitutionAdmin))
            throw new ForbiddenException("Only administrators can assign coaches.");

        // SuperAdmin bypasses institution check but must have a target institution
        Guid institutionId;
        if (_currentUser.IsSuperAdmin)
        {
            // For SuperAdmin: derive institution from the student being assigned
            var studentInst = await _dbContext.Set<Student>()
                .Where(s => s.Id == command.StudentId && !s.IsDeleted)
                .Select(s => (Guid?)s.InstitutionId)
                .FirstOrDefaultAsync(ct);

            institutionId = studentInst ?? throw new NotFoundException("Student", command.StudentId);
        }
        else
        {
            institutionId = await _currentUser.GetInstitutionIdAsync()
                ?? throw new ForbiddenException("User has no institution context.");
        }

        // Verify student exists in the target institution
        var student = await _dbContext.Set<Student>()
            .Where(s => s.Id == command.StudentId && !s.IsDeleted)
            .Select(s => new { s.InstitutionId })
            .FirstOrDefaultAsync(ct);

        if (student is null)
            throw new NotFoundException("Student", command.StudentId);

        if (student.InstitutionId != institutionId)
            throw new ForbiddenException("Cross-institution student assignment is not allowed.");

        // Verify coach exists in the target institution
        var coach = await _dbContext.Set<Coach>()
            .Where(c => c.Id == command.CoachId && !c.IsDeleted)
            .Select(c => new { c.InstitutionId })
            .FirstOrDefaultAsync(ct);

        if (coach is null)
            throw new NotFoundException("Coach", command.CoachId);

        if (coach.InstitutionId != institutionId)
            throw new ForbiddenException("Cross-institution coach assignment is not allowed.");

        // Check for duplicate active assignment
        var existingActive = await _dbContext.Set<StudentCoachAssignment>()
            .AnyAsync(a => a.StudentId == command.StudentId
                        && a.CoachId == command.CoachId
                        && a.IsActive, ct);

        if (existingActive)
            throw new ConflictException("This student is already actively assigned to this coach.");

        // If IsPrimary, deactivate other primary assignments for this student
        if (command.IsPrimary)
        {
            var currentPrimaries = await _dbContext.Set<StudentCoachAssignment>()
                .Where(a => a.StudentId == command.StudentId
                         && a.IsPrimary
                         && a.IsActive)
                .ToListAsync(ct);

            foreach (var primary in currentPrimaries)
            {
                primary.IsActive = false;
                primary.IsPrimary = false;
                primary.EndedAt = DateTimeOffset.UtcNow;
            }
        }

        var assignment = new StudentCoachAssignment
        {
            StudentId = command.StudentId,
            CoachId = command.CoachId,
            InstitutionId = institutionId,
            IsPrimary = command.IsPrimary,
            IsActive = true,
            AssignedAt = DateTimeOffset.UtcNow,
            CreatedBy = _currentUser.UserId,
        };

        _dbContext.Set<StudentCoachAssignment>().Add(assignment);

        // P2-02: Atomic audit — add to context, save together
        if (_currentUser.UserId.HasValue)
        {
            await _auditService.AddPendingLogAsync(
                userId: _currentUser.UserId.Value,
                action: "StudentCoach.Assigned",
                entityType: "StudentCoachAssignment",
                entityId: assignment.Id.ToString(),
                institutionId: institutionId,
                metadataJson: $"{{\"studentId\":\"{command.StudentId}\",\"coachId\":\"{command.CoachId}\",\"isPrimary\":{command.IsPrimary.ToString().ToLower()}}}");
        }

        // P2-05: Single atomic save with concurrent active-primary conflict handling.
        // The DB has a filtered unique index [IsPrimary]=1 AND [IsActive]=1 on StudentId.
        // If two concurrent requests try to create active primary assignments for the same
        // student, the DB rejects the second — we catch this and return 409 Conflict.
        // The ExceptionHandlingMiddleware also catches DbUpdateException globally as a fallback.
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException("Another active primary coach assignment for this student already exists. Please retry.");
        }
    }

    /// <summary>
    /// P2-05: Detects SQL Server unique constraint violation errors.
    /// SQL Server error 2601 = unique index violation, 2627 = unique constraint violation.
    /// Uses type name check to avoid requiring Microsoft.Data.SqlClient package reference.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        if (inner is null) return false;

        // Check by type name to avoid assembly dependency on SqlClient
        var typeName = inner.GetType().FullName ?? string.Empty;
        if (!typeName.Contains("SqlException")) return false;

        // SQL Server error numbers for unique constraint violations
        var numberProperty = inner.GetType().GetProperty("Number");
        if (numberProperty?.GetValue(inner) is int number)
            return number is 2601 or 2627;

        return false;
    }
}
