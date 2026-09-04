using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.ListStudentGoals;

public class ListStudentGoalsHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public ListStudentGoalsHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICoachStudentQuery coachStudentQuery)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _coachStudentQuery = coachStudentQuery;
    }

    public async Task<IReadOnlyList<StudentGoalDto>> HandleAsync(ListStudentGoalsQuery query, CancellationToken ct = default)
    {
        var student = await GoalAuthorizationHelper.FetchStudentOrThrowAsync(_dbContext, query.StudentId, ct);
        await GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        var source = _dbContext.Set<StudentGoal>()
            .AsNoTracking()
            .Where(g => g.StudentId == query.StudentId);

        if (query.IsActive.HasValue)
            source = source.Where(g => g.IsActive == query.IsActive.Value);

        var goals = await source
            .OrderByDescending(g => g.EffectiveDate)
            .ToListAsync(ct);

        return goals.Select(g => new StudentGoalDto(
            g.Id, g.StudentId, g.Title, g.Description, g.TargetExamTypeId,
            g.TargetScore, g.TargetRank, g.TargetSchoolName, g.EffectiveDate,
            g.IsActive, g.CreatedAt)).ToList();
    }
}
