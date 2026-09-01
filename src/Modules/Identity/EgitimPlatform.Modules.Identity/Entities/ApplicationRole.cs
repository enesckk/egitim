using Microsoft.AspNetCore.Identity;

namespace EgitimPlatform.Modules.Identity.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
