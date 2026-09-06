using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;
using Microsoft.EntityFrameworkCore;
namespace EgitimPlatform.Modules.Coaching.Services;
public class StudentCoachSummaryQuery(IApplicationDbContext db) : IStudentCoachSummaryQuery
{
    public async Task<IReadOnlyList<StudentCoachSummaryDto>> GetAsync(Guid studentId, Guid institutionId, CancellationToken ct = default) =>
        await (from a in db.Set<StudentCoachAssignment>().AsNoTracking()
               join c in db.Set<Coach>() on a.CoachId equals c.Id
               where a.StudentId == studentId && a.InstitutionId == institutionId && c.InstitutionId == institutionId && a.IsActive
               orderby a.IsPrimary descending, c.LastName, c.Id
               select new StudentCoachSummaryDto(c.Id, c.FirstName + " " + c.LastName, a.IsPrimary, a.AssignedAt)).ToListAsync(ct);
}
