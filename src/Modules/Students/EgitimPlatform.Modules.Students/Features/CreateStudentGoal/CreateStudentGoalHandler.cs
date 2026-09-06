using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EgitimPlatform.Modules.Students.Features.CreateStudentGoal;

public class CreateStudentGoalHandler
{
    private readonly IAcademicCatalog _academic;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public CreateStudentGoalHandler(
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

    public async Task<StudentGoalDto> HandleAsync(CreateStudentGoalCommand command, CancellationToken ct = default)
    {
        var institutionId = await _currentUser.GetInstitutionIdAsync();

        // Verify student exists and authorize
        var student = await _dbContext.Set<Student>()
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, ct);

        if (student is null)
            throw new NotFoundException("Student", command.StudentId);

        await EgitimPlatform.Modules.Students.Services.GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        if (command.TargetExamTypeId.HasValue && await _academic.GetExamTypeAsync(command.TargetExamTypeId.Value, ct) is null)
            throw new FluentValidation.ValidationException("Unknown or inactive exam type.");

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
        _dbContext.Set<StudentGoalHistory>().Add(EgitimPlatform.Modules.Students.Services.GoalHistory.Capture(goal, "Created", _currentUser.UserId));

        // Audit + save atomically
        if (_currentUser.UserId.HasValue)
        {
            await _auditService.AddPendingLogAsync(
                userId: _currentUser.UserId.Value,
                action: "StudentGoal.Created",
                entityType: "StudentGoal",
                entityId: goal.Id.ToString(),
                institutionId: goal.InstitutionId,
                metadataJson: JsonSerializer.Serialize(new { goal.StudentId }));
        }

        await _dbContext.SaveChangesAsync(ct);

        return ToDto(goal);
    }

    private static StudentGoalDto ToDto(StudentGoal g) => new(
        g.Id, g.StudentId, g.Title, g.Description, g.TargetExamTypeId,
        g.TargetScore, g.TargetRank, g.TargetSchoolName, g.EffectiveDate,
        g.IsActive, g.CreatedAt);
}
