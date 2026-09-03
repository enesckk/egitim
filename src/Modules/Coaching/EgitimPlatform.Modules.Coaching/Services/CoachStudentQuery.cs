using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Coaching.Services;

/// <summary>
/// P1-01 CLOSURE: Resolves (applicationUserId, institutionId) → Coach.Id with
/// institution-scoped lookup. Both parameters are REQUIRED — institution-less
/// users or mismatched user/institution pairs FAIL CLOSED (no profile found).
/// </summary>
public class CoachStudentQuery : ICoachStudentQuery
{
    private readonly IApplicationDbContext _dbContext;

    public CoachStudentQuery(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> GetActiveAssignedStudentIdsAsync(
        Guid applicationUserId, Guid institutionId, CancellationToken ct = default)
    {
        var coachId = await ResolveCoachIdAsync(applicationUserId, institutionId, ct);
        if (coachId is null) return Array.Empty<Guid>();

        return await _dbContext.Set<StudentCoachAssignment>()
            .Where(a => a.CoachId == coachId.Value
                     && a.InstitutionId == institutionId
                     && a.IsActive)
            .Select(a => a.StudentId)
            .ToListAsync(ct);
    }

    public async Task<bool> HasActiveAssignmentAsync(
        Guid applicationUserId, Guid institutionId, Guid studentId, CancellationToken ct = default)
    {
        var coachId = await ResolveCoachIdAsync(applicationUserId, institutionId, ct);
        if (coachId is null) return false;

        return await _dbContext.Set<StudentCoachAssignment>()
            .AnyAsync(a => a.CoachId == coachId.Value
                        && a.InstitutionId == institutionId
                        && a.StudentId == studentId
                        && a.IsActive, ct);
    }

    /// <summary>
    /// Resolves (ApplicationUser.Id, InstitutionId) → Coach.Id.
    /// Both must match exactly — prevents multi-profile ambiguity.
    /// Returns null if:
    /// - No coach profile exists for this user
    /// - Coach profile exists but with different InstitutionId
    /// - Coach profile is soft-deleted
    /// </summary>
    private async Task<Guid?> ResolveCoachIdAsync(Guid applicationUserId, Guid institutionId, CancellationToken ct)
    {
        return await _dbContext.Set<Coach>()
            .Where(c => c.UserId == applicationUserId
                     && c.InstitutionId == institutionId
                     && !c.IsDeleted)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);
    }
}
