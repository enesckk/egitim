using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EgitimPlatform.Modules.Students.Features.UpdateStudentGoal;

public class UpdateStudentGoalHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public UpdateStudentGoalHandler(
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

    public async Task<StudentGoalDto> HandleAsync(UpdateStudentGoalCommand command, CancellationToken ct = default)
    {
        var goal = await _dbContext.Set<StudentGoal>()
            .FirstOrDefaultAsync(g => g.Id == command.GoalId, ct)
            ?? throw new EgitimPlatform.BuildingBlocks.Exceptions.NotFoundException("StudentGoal", command.GoalId);

        var student = await GoalAuthorizationHelper.FetchStudentOrThrowAsync(_dbContext, goal.StudentId, ct);
        await GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        // Snapshot previous values for history
        var previousValues = JsonSerializer.Serialize(new
        {
            goal.Title, goal.Description, goal.TargetScore,
            goal.TargetRank, goal.TargetSchoolName, goal.EffectiveDate
        });

        // Apply updates (only non-null fields)
        if (command.Title is not null) goal.Title = command.Title;
        if (command.Description is not null) goal.Description = command.Description;
        if (command.TargetExamTypeId.HasValue) goal.TargetExamTypeId = command.TargetExamTypeId;
        if (command.TargetScore.HasValue) goal.TargetScore = command.TargetScore;
        if (command.TargetRank.HasValue) goal.TargetRank = command.TargetRank;
        if (command.TargetSchoolName is not null) goal.TargetSchoolName = command.TargetSchoolName;
        if (command.EffectiveDate.HasValue) goal.EffectiveDate = command.EffectiveDate.Value;

        // History entry
        var newValues = JsonSerializer.Serialize(new
        {
            goal.Title, goal.Description, goal.TargetScore,
            goal.TargetRank, goal.TargetSchoolName, goal.EffectiveDate
        });

        _dbContext.Set<StudentGoalHistory>().Add(new StudentGoalHistory
        {
            StudentGoalId = goal.Id,
            Action = "Updated",
            PreviousValuesJson = previousValues,
            NewValuesJson = newValues,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = _currentUser.UserId,
        });

        // Audit
        if (_currentUser.UserId.HasValue)
        {
            await _auditService.AddPendingLogAsync(
                userId: _currentUser.UserId.Value,
                action: "StudentGoal.Updated",
                entityType: "StudentGoal",
                entityId: goal.Id.ToString(),
                institutionId: goal.InstitutionId);
        }

        await _dbContext.SaveChangesAsync(ct);

        return ToDto(goal);
    }

    private static StudentGoalDto ToDto(StudentGoal g) => new(
        g.Id, g.StudentId, g.Title, g.Description, g.TargetExamTypeId,
        g.TargetScore, g.TargetRank, g.TargetSchoolName, g.EffectiveDate,
        g.IsActive, g.CreatedAt);
}
