using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Teachers.Configuration;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("Teachers");
        builder.Property(t => t.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.LastName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(50);

        builder.HasIndex(t => t.InstitutionId);
        builder.HasIndex(t => t.IsDeleted);

        // Sprint 2: One teacher profile per user per institution (like Coach pattern)
        builder.HasIndex(t => new { t.UserId, t.InstitutionId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Alternate key for composite FK from TeacherSubject
        builder.HasAlternateKey(t => new { t.Id, t.InstitutionId });
    }
}
