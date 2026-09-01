using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.CreateStudent;

public class CreateStudentHandler
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly IStudentCoachAssigner _studentCoachAssigner;

    public CreateStudentHandler(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditService auditService,
        IStudentCoachAssigner studentCoachAssigner)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditService = auditService;
        _studentCoachAssigner = studentCoachAssigner;
    }

    public async Task<StudentDto> HandleAsync(CreateStudentCommand command, CancellationToken ct = default)
    {
        var institutionId = await _currentUser.GetInstitutionIdAsync()
            ?? throw new ForbiddenException("User has no institution context.");

        // Check for duplicate student in the same institution
        var exists = await _dbContext.Set<Student>()
            .AnyAsync(s => s.InstitutionId == institutionId
                        && s.FirstName == command.FirstName
                        && s.LastName == command.LastName
                        && !s.IsDeleted, ct);

        if (exists)
            throw new ConflictException($"A student with name '{command.FirstName} {command.LastName}' already exists in this institution.");

        var student = new Student
        {
            InstitutionId = institutionId,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Status = StudentStatus.Active,
            CreatedBy = _currentUser.UserId,
        };

        _dbContext.Set<Student>().Add(student);

        // P1-02: If a Coach is creating the student, auto-assign them as primary coach.
        // This is server-side — client cannot specify a different CoachId.
        // Done in the same transaction as student creation (atomic).
        if (_currentUser.IsInRole(Roles.Coach) && _currentUser.UserId.HasValue)
        {
            var coachId = await _studentCoachAssigner.FindCoachIdByUserIdAsync(_currentUser.UserId.Value, ct);
            if (coachId.HasValue)
            {
                // Student.Id will be set after SaveChanges; but we need the ID for the assignment.
                // We'll create the assignment after the first save.
                // Alternative: use DB-generated GUID (already generated client-side via CreateVersion7).
                _studentCoachAssigner.CreatePrimaryAssignment(
                    student.Id, coachId.Value, institutionId, _currentUser.UserId);
            }
        }

        // P2-02: Audit record added to context but NOT saved separately.
        // Everything saved atomically in one SaveChangesAsync.
        if (_currentUser.UserId.HasValue)
        {
            await _auditService.AddPendingLogAsync(
                userId: _currentUser.UserId.Value,
                action: "Student.Created",
                entityType: "Student",
                entityId: student.Id.ToString(),
                institutionId: institutionId);
        }

        // Single atomic save: student + assignment + audit log
        await _dbContext.SaveChangesAsync(ct);

        return student.ToDto();
    }
}
