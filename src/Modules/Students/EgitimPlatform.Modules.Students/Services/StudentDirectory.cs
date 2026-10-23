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

    public async Task<StudentReferenceDto?> GetStudentByUserIdAsync(Guid userId, Guid institutionId, CancellationToken ct = default)
    {
        return await db.Set<Student>()
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.InstitutionId == institutionId && !s.IsDeleted)
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

    public async Task<IReadOnlyList<StudentImportLookupDto>> GetActiveStudentsForImportAsync(Guid institutionId, CancellationToken ct = default)
    {
        return await db.Set<Student>()
            .AsNoTracking()
            .Where(s => s.InstitutionId == institutionId && !s.IsDeleted)
            .Select(s => new StudentImportLookupDto(s.Id, s.InstitutionId, s.StudentNumber, s.FirstName, s.LastName))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StudentSummaryDto>> GetStudentsByIdsAsync(IEnumerable<Guid> studentIds, Guid institutionId, CancellationToken ct = default)
    {
        var ids = studentIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return await db.Set<Student>()
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id) && s.InstitutionId == institutionId && !s.IsDeleted)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentSummaryDto(s.Id, s.InstitutionId, s.FirstName, s.LastName, s.StudentNumber, s.GradeLevel))
            .ToListAsync(ct);
    }
}
