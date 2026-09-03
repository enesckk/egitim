using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;

using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Coaching.Services;

public class StudentCoachAssigner : IStudentCoachAssigner
{
    private readonly IApplicationDbContext _dbContext;

    public StudentCoachAssigner(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void CreatePrimaryAssignment(Guid studentId, Guid coachId, Guid institutionId, Guid? createdBy)
    {
        var assignment = new StudentCoachAssignment
        {
            StudentId = studentId,
            CoachId = coachId,
            InstitutionId = institutionId,
            IsPrimary = true,
            IsActive = true,
            AssignedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
        };

        _dbContext.Set<StudentCoachAssignment>().Add(assignment);
    }

    public async Task<Guid?> FindCoachIdByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Set<Coach>()
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);
    }
}
