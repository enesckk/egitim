using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Students.Entities;

public class Student : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public StudentStatus Status { get; set; } = StudentStatus.Active;
}
