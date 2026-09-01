using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Features.GetStudent;

public class GetStudentHandler
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetStudentHandler(ApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<StudentDto> HandleAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await _dbContext.Set<Student>()
            .Where(s => s.Id == studentId)
            .Select(s => new { s, s.InstitutionId })
            .FirstOrDefaultAsync(ct);

        if (student is null)
            throw new NotFoundException("Student", studentId);

        // Institution isolation check
        var userInstitutionId = await _currentUser.GetInstitutionIdAsync();
        if (!_currentUser.IsSuperAdmin && student.InstitutionId != userInstitutionId)
            throw new ForbiddenException("Access denied to this student.");

        return student.s.ToDto();
    }
}
