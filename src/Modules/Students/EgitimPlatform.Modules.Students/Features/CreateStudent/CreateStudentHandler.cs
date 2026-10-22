using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;

using EgitimPlatform.Modules.Students.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.CreateStudent;

public class CreateStudentHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly IStudentCoachAssigner _studentCoachAssigner;
    private readonly IUserDirectory? _userDirectory;
    private readonly IUserProvisioner? _userProvisioner;

    public CreateStudentHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditService auditService,
        IStudentCoachAssigner studentCoachAssigner,
        IUserDirectory? userDirectory = null,
        IUserProvisioner? userProvisioner = null)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditService = auditService;
        _studentCoachAssigner = studentCoachAssigner;
        _userDirectory = userDirectory;
        _userProvisioner = userProvisioner;
    }

    public async Task<StudentDto> HandleAsync(CreateStudentCommand command, CancellationToken ct = default)
    {
        var institutionId = await _currentUser.GetInstitutionIdAsync()
            ?? throw new ForbiddenException("User has no institution context.");

        if (command.UserId.HasValue)
        {
            if (_userDirectory is not null && !await _userDirectory.IsActiveInInstitutionAsync(command.UserId.Value, institutionId, Roles.Student, ct))
                throw new ValidationException("Student account must be active in the same institution with the Student role.");

            var userLinked = await _dbContext.Set<Student>()
                .AnyAsync(s => s.InstitutionId == institutionId && s.UserId == command.UserId.Value && !s.IsDeleted, ct);
            if (userLinked)
                throw new ConflictException("This user account is already linked to a student profile in this institution.");
        }

        if (!string.IsNullOrWhiteSpace(command.StudentNumber))
        {
            var numberExists = await _dbContext.Set<Student>()
                .AnyAsync(s => s.InstitutionId == institutionId && s.StudentNumber == command.StudentNumber.Trim() && !s.IsDeleted, ct);
            if (numberExists)
                throw new ConflictException($"A student with number '{command.StudentNumber.Trim()}' already exists in this institution.");
        }

        // Check for duplicate student in the same institution
        var exists = await _dbContext.Set<Student>()
            .AnyAsync(s => s.InstitutionId == institutionId
                        && s.FirstName == command.FirstName
                        && s.LastName == command.LastName
                        && !s.IsDeleted, ct);

        if (exists)
            throw new ConflictException($"A student with name '{command.FirstName} {command.LastName}' already exists in this institution.");

        Guid? targetUserId = command.UserId;
        Guid? provisionedUserId = null;

        await using var transaction = await _dbContext.BeginTransactionAsync(ct);
        try
        {
            if (!targetUserId.HasValue && !string.IsNullOrWhiteSpace(command.Email) && !string.IsNullOrWhiteSpace(command.Password))
            {
                if (_userProvisioner is null)
                    throw new InvalidOperationException("User provisioning service is unavailable.");

                provisionedUserId = await _userProvisioner.ProvisionUserAsync(
                    command.Email,
                    command.Password,
                    command.FirstName,
                    command.LastName,
                    Roles.Student,
                    institutionId,
                    ct);

                targetUserId = provisionedUserId;
            }

            var student = new Student
            {
                InstitutionId = institutionId,
                UserId = targetUserId,
                FirstName = command.FirstName,
                LastName = command.LastName,
                SchoolName = string.IsNullOrWhiteSpace(command.SchoolName) ? null : command.SchoolName.Trim(),
                StudentNumber = string.IsNullOrWhiteSpace(command.StudentNumber) ? null : command.StudentNumber.Trim(),
                GradeLevel = command.GradeLevel,
                EnrollmentDate = (command.SchoolName is not null || command.StudentNumber is not null || command.GradeLevel.HasValue)
                    ? DateTimeOffset.UtcNow
                    : null,
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
            await transaction.CommitAsync(ct);

            return student.ToDto();
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            if (provisionedUserId.HasValue && _userProvisioner is not null)
            {
                try
                {
                    await _userProvisioner.DeleteProvisionedUserAsync(provisionedUserId.Value, ct);
                }
                catch
                {
                    // Secondary compensating cleanup attempt; DB transaction rollback already guaranteed database consistency.
                }
            }
            throw;
        }
    }
}
