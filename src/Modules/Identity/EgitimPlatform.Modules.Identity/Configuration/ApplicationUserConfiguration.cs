using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Identity.Configuration;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.InstitutionId).HasFilter("[InstitutionId] IS NOT NULL");
        builder.HasIndex(u => u.IsDeleted);
        builder.HasIndex(u => u.Email).HasDatabaseName("IX_Users_Email").HasFilter("[Email] IS NOT NULL");
    }
}
