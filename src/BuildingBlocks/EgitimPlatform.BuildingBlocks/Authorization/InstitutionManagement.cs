using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
namespace EgitimPlatform.BuildingBlocks.Authorization;
public static class InstitutionManagement
{
    public static async Task DemandAsync(ICurrentUser user, Guid institutionId)
    {
        if (!user.IsAuthenticated || user.UserId is null) throw new ForbiddenException("Access denied.");
        if (user.IsSuperAdmin) return;
        if (!user.IsInRole(Roles.InstitutionAdmin) || await user.GetInstitutionIdAsync() != institutionId)
            throw new ForbiddenException("Access denied.");
    }
}
