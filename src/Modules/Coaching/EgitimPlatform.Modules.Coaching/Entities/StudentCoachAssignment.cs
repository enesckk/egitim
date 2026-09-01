using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Coaching.Entities;

public class StudentCoachAssignment : AuditableEntity, IHasInstitutionId
{
    public Guid StudentId { get; set; }
    public Guid CoachId { get; set; }
    public Guid InstitutionId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
}
