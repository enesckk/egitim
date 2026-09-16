using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Students.Entities;

/// <summary>
/// Sprint 2 — Join entity linking a Parent to a Student within the same institution.
/// Relationship type is recorded (Mother, Father, Guardian, etc.)
/// Active/inactive state supports history without destructive deletes.
/// </summary>
public class StudentParent : SoftDeletableEntity, IHasInstitutionId
{
    public Guid StudentId { get; set; }
    public Guid ParentId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Guid InstitutionId { get; set; }

    /// <summary>Relationship type: Mother, Father, Guardian, etc.</summary>
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>Whether this relationship is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Optional notes about the relationship.</summary>
    public string? Notes { get; set; }
}
