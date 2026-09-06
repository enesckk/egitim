using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Teachers.Entities;

/// <summary>
/// Sprint 2 — Teacher domain entity.
/// Maps to an ApplicationUser via UserId (non-nullable, like Coach).
/// Institution-scoped with filtered unique index on (UserId, InstitutionId).
/// Teacher access to student data FAILS CLOSED without explicit student relationship.
/// </summary>
public class Teacher : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }

    /// <summary>Non-nullable — every teacher profile maps to a real user.</summary>
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Optional title (e.g. "Dr.", "Prof."). Free text.</summary>
    public string? Title { get; set; }
}
