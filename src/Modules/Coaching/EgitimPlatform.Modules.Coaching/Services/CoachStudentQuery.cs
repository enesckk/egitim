using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Coaching.Services;

public class CoachStudentQuery : ICoachStudentQuery
{
    private readonly ApplicationDbContext _dbContext;

    public CoachStudentQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> GetActiveAssignedStudentIdsAsync(Guid coachId, CancellationToken ct = default)
    {
        return await _dbContext.Set<StudentCoachAssignment>()
            .Where(a => a.CoachId == coachId && a.IsActive)
            .Select(a => a.StudentId)
            .ToListAsync(ct);
    }

    public async Task<bool> HasActiveAssignmentAsync(Guid coachId, Guid studentId, CancellationToken ct = default)
    {
        return await _dbContext.Set<StudentCoachAssignment>()
            .AnyAsync(a => a.CoachId == coachId
                        && a.StudentId == studentId
                        && a.IsActive, ct);
    }
}
