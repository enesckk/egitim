using EgitimPlatform.BuildingBlocks.Entities;

namespace EgitimPlatform.Modules.Students.Entities;

/// <summary>
/// Sprint 2 — Immutable audit trail for StudentGoal changes.
/// Every create/update/deactivation/reactivation of a goal produces a history row.
/// History rows are NEVER soft-deleted or modified — they are append-only.
/// </summary>
public class StudentGoalHistory : BaseEntity
{
    public Guid StudentGoalId { get; set; }

    /// <summary>What changed: Created, Updated, Deactivated, Reactivated.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>JSON snapshot of previous values (null for Created).</summary>
    public string? PreviousValuesJson { get; set; }

    /// <summary>JSON snapshot of new values.</summary>
    public string? NewValuesJson { get; set; }

    /// <summary>When the change occurred.</summary>
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Who made the change (ApplicationUser.Id).</summary>
    public Guid? ChangedBy { get; set; }

    /// <summary>Correlation ID for request tracing.</summary>
    public string? CorrelationId { get; set; }
}
