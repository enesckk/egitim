using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.GetStudentGoal;

public class GetStudentGoalHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public GetStudentGoalHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICoachStudentQuery coachStudentQuery)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _coachStudentQuery = coachStudentQuery;
    }

    public async Task<StudentGoalDto> HandleAsync(Guid goalId, CancellationToken ct = default)
    {
        var institutionId = await _currentUser.GetInstitutionIdAsync();
        if (!_currentUser.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var goals = _dbContext.Set<StudentGoal>().AsNoTracking().Where(g => g.Id == goalId);
        if (!_currentUser.IsSuperAdmin)
            goals = goals.Where(g => g.InstitutionId == institutionId!.Value);

        var goal = await goals
            .Select(g => new StudentGoalDto(
                g.Id, g.StudentId, g.Title, g.Description, g.TargetExamTypeId,
                g.TargetScore, g.TargetRank, g.TargetSchoolName, g.EffectiveDate,
                g.IsActive, g.CreatedAt))
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("StudentGoal", goalId);

        var student = await GoalAuthorizationHelper.FetchStudentOrThrowAsync(_dbContext, goal.StudentId, ct);
        await GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        return goal;
    }
}
