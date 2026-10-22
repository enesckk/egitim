namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface IUserProvisioner
{
    Task<Guid> ProvisionUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        Guid institutionId,
        CancellationToken ct = default);

    Task DeleteProvisionedUserAsync(Guid userId, CancellationToken ct = default);
}
