using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EgitimPlatform.Modules.Students.Features.CreateStudentGoal;

public class CreateStudentGoalHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public CreateStudentGoalHandler(
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

    public async Task<StudentGoalDto> HandleAsync(CreateStudentGoalCommand command, CancellationToken ct = default)
    {
        var institutionId = await _currentUser.GetInstitutionIdAsync();

        // Verify student exists and authorize
        var student = await _dbContext.Set<Student>()
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, ct);

        if (student is null)
            throw new NotFoundException("Student", command.StudentId);

        await AuthorizeAsync(student, institutionId, ct);

        var goal = new StudentGoal
        {
            InstitutionId = student.InstitutionId,
            StudentId = student.Id,
            Title = command.Title,
            Description = command.Description,
            TargetExamTypeId = command.TargetExamTypeId,
            TargetScore = command.TargetScore,
            TargetRank = command.TargetRank,
            TargetSchoolName = command.TargetSchoolName,
            EffectiveDate = command.EffectiveDate ?? DateTimeOffset.UtcNow,
            IsActive = true,
            CreatedBy = _currentUser.UserId,
        };

        _dbContext.Set<StudentGoal>().Add(goal);

        // History entry for creation
        var history = new StudentGoalHistory
        {
            StudentGoalId = goal.Id,
            Action = "Created",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                goal.Title, goal.Description, goal.TargetScore,
                goal.TargetRank, goal.TargetSchoolName, goal.EffectiveDate
            }),
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = _currentUser.UserId,
        };
        _dbContext.Set<StudentGoalHistory>().Add(history);

        // Audit + save atomically
        if (_currentUser.UserId.HasValue)
        {
            await _auditService.AddPendingLogAsync(
                userId: _currentUser.UserId.Value,
                action: "StudentGoal.Created",
                entityType: "StudentGoal",
                entityId: goal.Id.ToString(),
                institutionId: goal.InstitutionId,
                metadataJson: $"{{\"studentId\":\"{goal.StudentId}\",\"title\":\"{goal.Title}\"}}");
        }

        await _dbContext.SaveChangesAsync(ct);

        return ToDto(goal);
    }

    private async Task AuthorizeAsync(Student student, Guid? institutionId, CancellationToken ct)
    {
        if (_currentUser.IsSuperAdmin) return;

        if (_currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            if (!institutionId.HasValue || student.InstitutionId != institutionId.Value)
                throw new ForbiddenException("Access denied.");
            return;
        }

        if (_currentUser.IsInRole(Roles.Coach))
        {
            if (_currentUser.UserId is null || !institutionId.HasValue)
                throw new ForbiddenException("Access denied.");

            if (student.InstitutionId != institutionId.Value)
                throw new ForbiddenException("Access denied.");

            var hasAssignment = await _coachStudentQuery.HasActiveAssignmentAsync(
                _currentUser.UserId.Value, institutionId.Value, student.Id, ct);
            if (!hasAssignment)
                throw new ForbiddenException("Access denied.");
            return;
        }

        if (_currentUser.IsInRole(Roles.Student))
        {
            if (_currentUser.UserId is null || student.UserId != _currentUser.UserId.Value)
                throw new ForbiddenException("Access denied.");
            return;
        }

        throw new ForbiddenException("Access denied.");
    }

    private static StudentGoalDto ToDto(StudentGoal g) => new(
        g.Id, g.StudentId, g.Title, g.Description, g.TargetExamTypeId,
        g.TargetScore, g.TargetRank, g.TargetSchoolName, g.EffectiveDate,
        g.IsActive, g.CreatedAt);
}
