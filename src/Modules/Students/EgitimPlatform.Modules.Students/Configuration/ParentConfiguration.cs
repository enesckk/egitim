using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Students.Configuration;

public class ParentConfiguration : IEntityTypeConfiguration<Parent>
{
    public void Configure(EntityTypeBuilder<Parent> builder)
    {
        builder.ToTable("Parents");
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Phone).HasMaxLength(30);
        builder.Property(p => p.Email).HasMaxLength(200);

        builder.HasIndex(p => p.InstitutionId);
        builder.HasIndex(p => p.UserId).HasFilter("[UserId] IS NOT NULL");
        builder.HasIndex(p => p.IsDeleted);

        // Sprint 2: One parent profile per user per institution (filtered unique)
        builder.HasIndex(p => new { p.UserId, p.InstitutionId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");

        // Alternate key for composite FK
        builder.HasAlternateKey(p => new { p.Id, p.InstitutionId });
    }
}
