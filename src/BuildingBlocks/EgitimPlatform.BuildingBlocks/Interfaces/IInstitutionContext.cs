namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface IInstitutionContext
{
    Guid? InstitutionId { get; }
    bool IsSuperAdmin { get; }
    bool IsResolved { get; }
}
