using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Teachers.Entities;

public class Classroom : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public int? GradeLevel { get; set; }
    public string? AcademicYear { get; set; }
}
