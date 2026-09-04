using EgitimPlatform.BuildingBlocks.Interfaces;
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
        var goal = await _dbContext.Set<StudentGoal>()
            .FirstOrDefaultAsync(g => g.Id == goalId, ct)
            ?? throw new EgitimPlatform.BuildingBlocks.Exceptions.NotFoundException("StudentGoal", goalId);

        var student = await GoalAuthorizationHelper.FetchStudentOrThrowAsync(_dbContext, goal.StudentId, ct);
        await GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        return ToDto(goal);
    }

    private static StudentGoalDto ToDto(StudentGoal g) => new(
        g.Id, g.StudentId, g.Title, g.Description, g.TargetExamTypeId,
        g.TargetScore, g.TargetRank, g.TargetSchoolName, g.EffectiveDate,
        g.IsActive, g.CreatedAt);
}
