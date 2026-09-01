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

        builder.HasIndex(p => p.InstitutionId);
        builder.HasIndex(p => p.UserId).HasFilter("[UserId] IS NOT NULL");
        builder.HasIndex(p => p.IsDeleted);
    }
}
