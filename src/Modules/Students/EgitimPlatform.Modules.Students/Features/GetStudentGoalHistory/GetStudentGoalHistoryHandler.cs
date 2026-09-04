using EgitimPlatform.BuildingBlocks.Interfaces;
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
        var goal = await _dbContext.Set<StudentGoal>()
            .FirstOrDefaultAsync(g => g.Id == goalId, ct)
            ?? throw new EgitimPlatform.BuildingBlocks.Exceptions.NotFoundException("StudentGoal", goalId);

        var student = await GoalAuthorizationHelper.FetchStudentOrThrowAsync(_dbContext, goal.StudentId, ct);
        await GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        var history = await _dbContext.Set<StudentGoalHistory>()
            .AsNoTracking()
            .Where(h => h.StudentGoalId == goalId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new StudentGoalHistoryDto(
                h.Id, h.StudentGoalId, h.Action,
                h.PreviousValuesJson, h.NewValuesJson,
                h.ChangedAt, h.ChangedBy))
            .ToListAsync(ct);

        return history;
    }
}
