using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.GetStudentGoalHistory;

public class GetStudentGoalHistoryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public GetStudentGoalHistoryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICoachStudentQuery coachStudentQuery)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _coachStudentQuery = coachStudentQuery;
    }

    public async Task<IReadOnlyList<StudentGoalHistoryDto>> HandleAsync(Guid goalId, CancellationToken ct = default)
    {
        var goals = _dbContext.Set<StudentGoal>()
            .AsNoTracking()
            .Where(g => g.Id == goalId);

        if (!_currentUser.IsSuperAdmin)
        {
            var institutionId = await _currentUser.GetInstitutionIdAsync();
            if (!institutionId.HasValue)
                throw new ForbiddenException("Access denied.");

            goals = goals.Where(g => g.InstitutionId == institutionId.Value);
        }

        var goal = await goals.SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("StudentGoal", goalId);

        var student = await _dbContext.Set<Student>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == goal.StudentId && s.InstitutionId == goal.InstitutionId,
                ct)
            ?? throw new NotFoundException("Student", goal.StudentId);
        await GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        var history = await _dbContext.Set<StudentGoalHistory>()
            .AsNoTracking()
            .Where(h => h.StudentGoalId == goalId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new StudentGoalHistoryDto(
                h.Id, h.StudentGoalId, h.Action,
                h.PreviousValuesJson, h.NewValuesJson,
                h.ChangedAt, h.ChangedBy, h.CorrelationId))
            .ToListAsync(ct);

        return history;
    }
}
