using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Services;

public class StudentDirectory(IApplicationDbContext db) : IStudentDirectory
{
    public async Task<StudentReferenceDto?> GetStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        return await db.Set<Student>()
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new StudentReferenceDto(s.Id, s.InstitutionId, s.UserId))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<bool> IsParentRelatedToStudentAsync(Guid parentUserId, Guid studentId, Guid institutionId, CancellationToken ct = default)
    {
        return await (from link in db.Set<StudentParent>().AsNoTracking()
                      join parent in db.Set<Parent>().AsNoTracking() on link.ParentId equals parent.Id
                      where link.StudentId == studentId && link.InstitutionId == institutionId && link.IsActive &&
                            parent.InstitutionId == institutionId && parent.UserId == parentUserId
                      select link.Id).AnyAsync(ct);
    }
}
