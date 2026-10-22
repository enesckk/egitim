using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Teachers.Services;

public class TeacherStudentQuery(IApplicationDbContext db) : ITeacherStudentQuery
{
    public async Task<IReadOnlyList<Guid>> GetActiveAssignedStudentIdsAsync(
        Guid applicationUserId, Guid institutionId, CancellationToken ct = default)
    {
        var teacher = await db.Set<Teacher>()
            .AsNoTracking()
            .Where(t => t.UserId == applicationUserId && t.InstitutionId == institutionId && !t.IsDeleted)
            .Select(t => new { t.Id })
            .FirstOrDefaultAsync(ct);

        if (teacher is null) return [];

        return await (from ctLink in db.Set<ClassroomTeacher>().AsNoTracking()
                      join c in db.Set<Classroom>().AsNoTracking() on ctLink.ClassroomId equals c.Id
                      join cs in db.Set<ClassroomStudent>().AsNoTracking() on c.Id equals cs.ClassroomId
                      where ctLink.TeacherId == teacher.Id
                            && ctLink.InstitutionId == institutionId
                            && !ctLink.IsDeleted
                            && c.InstitutionId == institutionId
                            && !c.IsDeleted
                            && cs.InstitutionId == institutionId
                            && !cs.IsDeleted
                      select cs.StudentId)
                      .Distinct()
                      .ToListAsync(ct);
    }

    public async Task<bool> HasActiveAssignmentAsync(
        Guid applicationUserId, Guid institutionId, Guid studentId, CancellationToken ct = default)
    {
        var teacher = await db.Set<Teacher>()
            .AsNoTracking()
            .Where(t => t.UserId == applicationUserId && t.InstitutionId == institutionId && !t.IsDeleted)
            .Select(t => new { t.Id })
            .FirstOrDefaultAsync(ct);

        if (teacher is null) return false;

        return await (from ctLink in db.Set<ClassroomTeacher>().AsNoTracking()
                      join c in db.Set<Classroom>().AsNoTracking() on ctLink.ClassroomId equals c.Id
                      join cs in db.Set<ClassroomStudent>().AsNoTracking() on c.Id equals cs.ClassroomId
                      where ctLink.TeacherId == teacher.Id
                            && ctLink.InstitutionId == institutionId
                            && !ctLink.IsDeleted
                            && c.InstitutionId == institutionId
                            && !c.IsDeleted
                            && cs.StudentId == studentId
                            && cs.InstitutionId == institutionId
                            && !cs.IsDeleted
                      select cs.Id)
                      .AnyAsync(ct);
    }
}
