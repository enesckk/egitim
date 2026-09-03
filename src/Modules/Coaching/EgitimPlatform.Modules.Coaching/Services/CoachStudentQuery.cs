using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;

using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Coaching.Services;

public class CoachStudentQuery : ICoachStudentQuery
{
    private readonly IApplicationDbContext _dbContext;

    public CoachStudentQuery(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// P1-01: Resolves userId → Coach.Id internally, then queries assignments.
    /// Callers pass ApplicationUser.Id; we map to domain Coach.Id before hitting assignments.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetActiveAssignedStudentIdsByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var coachId = await ResolveCoachIdAsync(userId, ct);
        if (coachId is null) return Array.Empty<Guid>();

        return await _dbContext.Set<StudentCoachAssignment>()
            .Where(a => a.CoachId == coachId.Value && a.IsActive)
            .Select(a => a.StudentId)
            .ToListAsync(ct);
    }

    public async Task<bool> HasActiveAssignmentByUserAsync(Guid userId, Guid studentId, CancellationToken ct = default)
    {
        var coachId = await ResolveCoachIdAsync(userId, ct);
        if (coachId is null) return false;

        return await _dbContext.Set<StudentCoachAssignment>()
            .AnyAsync(a => a.CoachId == coachId.Value
                        && a.StudentId == studentId
                        && a.IsActive, ct);
    }

    /// <summary>
    /// Resolves ApplicationUser.Id → Coach.Id.
    /// Returns null if no coach profile exists for this user (e.g. user is not a Coach,
    /// or the coach entity was soft-deleted).
    /// </summary>
    private async Task<Guid?> ResolveCoachIdAsync(Guid userId, CancellationToken ct)
    {
        return await _dbContext.Set<Coach>()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);
    }
}
