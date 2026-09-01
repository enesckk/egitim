using EgitimPlatform.Modules.Institutions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Institutions.Configuration;

public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("Institutions");
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(i => i.Name).HasFilter("[IsDeleted] = 0");
        builder.HasIndex(i => i.IsDeleted);
    }
}
