using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Students.Entities;

/// <summary>
/// Sprint 2 — Parent entity extended with contact information.
/// Parent ↔ Student relationship is managed via StudentParent join entity.
/// </summary>
public class Parent : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Parent's phone number. Nullable — not every parent record has it.</summary>
    public string? Phone { get; set; }

    /// <summary>Parent's email. Nullable — independent of user account email.</summary>
    public string? Email { get; set; }
}
