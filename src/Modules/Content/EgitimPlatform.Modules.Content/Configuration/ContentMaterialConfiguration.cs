using EgitimPlatform.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Content.Configuration;

public class ContentMaterialConfiguration : IEntityTypeConfiguration<ContentMaterial>
{
    public void Configure(EntityTypeBuilder<ContentMaterial> builder)
    {
        builder.ToTable("ContentMaterials");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Subject).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Topic).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Grade).IsRequired().HasMaxLength(50);
        builder.Property(m => m.MaterialType).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Description).HasMaxLength(1000);

        builder.Property(m => m.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(m => m.StoragePath).IsRequired().HasMaxLength(500);
        builder.Property(m => m.ContentType).IsRequired().HasMaxLength(100);

        builder.HasIndex(m => m.InstitutionId);
        builder.HasIndex(m => new { m.InstitutionId, m.TeacherId });
        builder.HasIndex(m => new { m.InstitutionId, m.Subject });

        builder.HasMany(m => m.Assignments)
            .WithOne(a => a.Material)
            .HasForeignKey(a => a.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
