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

    public CreateStudentHandler(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditService = auditService;
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
        await _dbContext.SaveChangesAsync(ct);

        if (_currentUser.UserId.HasValue)
        {
            await _auditService.LogAsync(
                userId: _currentUser.UserId.Value,
                action: "Student.Created",
                entityType: "Student",
                entityId: student.Id.ToString(),
                institutionId: institutionId,
                cancellationToken: ct);
        }

        return student.ToDto();
    }
}
