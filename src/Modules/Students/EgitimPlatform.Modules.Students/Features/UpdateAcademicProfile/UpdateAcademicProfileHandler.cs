using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;

public class UpdateAcademicProfileHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly ICoachStudentQuery _coachStudentQuery;

    public UpdateAcademicProfileHandler(
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

    public async Task<StudentDto> HandleAsync(UpdateAcademicProfileCommand command, CancellationToken ct = default)
    {
        var institutionId = await _currentUser.GetInstitutionIdAsync();

        // Fetch student (query filter handles soft-delete)
        var student = await _dbContext.Set<Student>()
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, ct);

        if (student is null)
            throw new NotFoundException("Student", command.StudentId);

        // Authorization: who can update academic profile?
        await EgitimPlatform.Modules.Students.Services.GoalAuthorizationHelper.AuthorizeForStudentAsync(student, _currentUser, _coachStudentQuery, ct);

        // Check StudentNumber uniqueness within institution (if changed)
        if (command.StudentNumber is not null && command.StudentNumber != student.StudentNumber)
        {
            var duplicate = await _dbContext.Set<Student>()
                .AnyAsync(s => s.InstitutionId == student.InstitutionId
                            && s.StudentNumber == command.StudentNumber
                            && s.Id != student.Id, ct);
            if (duplicate)
                throw new ConflictException($"Student number '{command.StudentNumber}' already exists in this institution.");
        }

        // Update fields
        student.SchoolName = command.SchoolName;
        student.StudentNumber = command.StudentNumber;
        student.GradeLevel = command.GradeLevel;
        student.EnrollmentDate = command.EnrollmentDate;

        student.UpdatedBy = _currentUser.UserId;
        // Audit + save atomically
        if (_currentUser.UserId.HasValue)
        {
            await _auditService.AddPendingLogAsync(
                userId: _currentUser.UserId.Value,
                action: "Student.AcademicProfile.Updated",
                entityType: "Student",
                entityId: student.Id.ToString(),
                institutionId: student.InstitutionId,
                metadataJson: System.Text.Json.JsonSerializer.Serialize(new { student.GradeLevel }));
        }

        await _dbContext.SaveChangesAsync(ct);

        return student.ToDto();
    }

}
