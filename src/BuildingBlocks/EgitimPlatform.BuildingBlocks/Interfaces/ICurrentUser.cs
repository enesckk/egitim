namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
    string? Email { get; }
    Task<Guid?> GetInstitutionIdAsync();
    bool IsInRole(string role);
    bool IsSuperAdmin { get; }
}
