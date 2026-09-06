using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;
namespace EgitimPlatform.Modules.Identity.Auth;
public sealed class UserDirectory(IApplicationDbContext db) : IUserDirectory
{
    public Task<bool> IsActiveInInstitutionAsync(Guid userId, Guid institutionId, string role, CancellationToken ct = default) =>
        (from u in db.Set<ApplicationUser>()
         join ur in db.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>() on u.Id equals ur.UserId
         join r in db.Set<ApplicationRole>() on ur.RoleId equals r.Id
         where u.Id == userId && u.InstitutionId == institutionId && u.IsActive && !u.IsDeleted && r.Name == role && !r.IsDeleted
         select u.Id).AnyAsync(ct);
}
