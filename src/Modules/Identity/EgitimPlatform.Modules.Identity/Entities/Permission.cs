namespace EgitimPlatform.Modules.Identity.Entities;

public class Permission
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
