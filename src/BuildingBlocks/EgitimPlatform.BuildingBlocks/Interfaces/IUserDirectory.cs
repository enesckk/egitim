namespace EgitimPlatform.BuildingBlocks.Interfaces;
public interface IUserDirectory
{
    Task<bool> IsActiveInInstitutionAsync(Guid userId, Guid institutionId, string role, CancellationToken ct = default);
}
