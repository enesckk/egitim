using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Students.Configuration;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.LastName).HasMaxLength(100).IsRequired();

        // Sprint 2 — Academic profile field constraints
        builder.Property(s => s.SchoolName).HasMaxLength(200);
        builder.Property(s => s.StudentNumber).HasMaxLength(50);
        builder.Property(s => s.GradeLevel).HasDefaultValue(null);

        builder.HasIndex(s => s.InstitutionId);
        builder.HasIndex(s => s.UserId).HasFilter("[UserId] IS NOT NULL");
        builder.HasIndex(s => new { s.LastName, s.FirstName });
        builder.HasIndex(s => s.IsDeleted);
        builder.HasIndex(s => new { s.InstitutionId, s.IsDeleted });

        // Sprint 2: StudentNumber unique per institution (when set).
        // Filtered index: only applies when StudentNumber IS NOT NULL.
        builder.HasIndex(s => new { s.InstitutionId, s.StudentNumber })
            .IsUnique()
            .HasFilter("[StudentNumber] IS NOT NULL AND [IsDeleted] = 0");

        // Sprint 2: Index for grade-level queries within institution.
        builder.HasIndex(s => new { s.InstitutionId, s.GradeLevel });

        // P2-6: Alternate key for cross-tenant FK integrity.
        builder.HasAlternateKey(s => new { s.Id, s.InstitutionId });
    }
}
