using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EgitimPlatform.Modules.Students.Features.UpdateStudentGoal;

public class UpdateStudentGoalHandler
{
    private readonly IAcademicCatalog _academic;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public UpdateStudentGoalHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditService auditService,
        ICoachStudentQuery coachStudentQuery, IAcademicCatalog academic)
    {
        _academic = academic;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditService = auditService;
        _coachStudentQuery = coachStudentQuery;
    }

    public async Task<StudentGoalDto> HandleAsync(UpdateStudentGoalCommand command, CancellationToken ct = default)
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

        if (command.TargetExamTypeId.HasValue && await _academic.GetExamTypeAsync(command.TargetExamTypeId.Value, ct) is null)
            throw new FluentValidation.ValidationException("Unknown or inactive exam type.");

        // Snapshot previous values for history
        var previousValues = GoalHistory.Snapshot(goal);

        // Apply updates (only non-null fields)
        if (command.Title is not null) goal.Title = command.Title;
        if (command.Description is not null) goal.Description = command.Description;
        if (command.TargetExamTypeId.HasValue) goal.TargetExamTypeId = command.TargetExamTypeId;
        if (command.TargetScore.HasValue) goal.TargetScore = command.TargetScore;
        if (command.TargetRank.HasValue) goal.TargetRank = command.TargetRank;
        if (command.TargetSchoolName is not null) goal.TargetSchoolName = command.TargetSchoolName;
        if (command.EffectiveDate.HasValue) goal.EffectiveDate = command.EffectiveDate.Value;

        goal.UpdatedBy = _currentUser.UserId;
        _dbContext.Set<StudentGoalHistory>().Add(GoalHistory.Capture(goal, "Updated", _currentUser.UserId, previousValues));

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
