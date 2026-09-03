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
        // P1-01: One coach profile per user per institution (prevents unintended duplicates)
        builder.HasIndex(c => new { c.UserId, c.InstitutionId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(c => c.IsDeleted);

        // P2-6: Alternate key for cross-tenant FK integrity.
        // StudentCoachAssignment references Coach via (CoachId, InstitutionId) composite FK,
        // ensuring at DB level that an assignment's InstitutionId matches the Coach's InstitutionId.
        builder.HasAlternateKey(c => new { c.Id, c.InstitutionId });
    }
}
