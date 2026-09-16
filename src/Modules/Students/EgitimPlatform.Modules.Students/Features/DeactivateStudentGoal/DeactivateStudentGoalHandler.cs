using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EgitimPlatform.Modules.Students.Features.DeactivateStudentGoal;

public class DeactivateStudentGoalHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public DeactivateStudentGoalHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditService auditService,
        ICoachStudentQuery coachStudentQuery)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditService = auditService;
        _coachStudentQuery = coachStudentQuery;
    }

    public async Task HandleAsync(DeactivateStudentGoalCommand command, CancellationToken ct = default)
    {
        var goals = _dbContext.Set<StudentGoal>()
            .Where(g => g.Id == command.GoalId);

        if (!_currentUser.IsSuperAdmin)
        {
            var institutionId = await _currentUser.GetInstitutionIdAsync();
            if (!institutionId.HasValue)
                throw new ForbiddenException("Access denied.");

            goals = goals.Where(g => g.InstitutionId == institutionId.Value);
        }

        var goal = await goals.SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("StudentGoal", command.GoalId);

        var student = await _dbContext.Set<Student>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == goal.StudentId && s.InstitutionId == goal.InstitutionId,
                ct)
            ?? throw new NotFoundException("Student", goal.StudentId);
        await GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        if (!goal.IsActive) return; // Idempotency follows resource authorization.
        var previousValues = GoalHistory.Snapshot(goal);
        goal.IsActive = false;
        goal.UpdatedBy = _currentUser.UserId;
        _dbContext.Set<StudentGoalHistory>().Add(GoalHistory.Capture(goal, "Deactivated", _currentUser.UserId, previousValues));

        if (_currentUser.UserId.HasValue)
        {
            await _auditService.AddPendingLogAsync(
                userId: _currentUser.UserId.Value,
                action: "StudentGoal.Deactivated",
                entityType: "StudentGoal",
                entityId: goal.Id.ToString(),
                institutionId: goal.InstitutionId);
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
