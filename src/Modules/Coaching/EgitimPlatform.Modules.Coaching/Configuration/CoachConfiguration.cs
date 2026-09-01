using EgitimPlatform.Modules.Coaching.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Coaching.Configuration;

public class CoachConfiguration : IEntityTypeConfiguration<Coach>
{
    public void Configure(EntityTypeBuilder<Coach> builder)
    {
        builder.ToTable("Coaches");
        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();

        builder.HasIndex(c => c.InstitutionId);
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.IsDeleted);
    }
}
