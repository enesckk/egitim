using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Teachers.Configuration;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.ToTable("Classrooms");
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Branch).HasMaxLength(50);
        builder.Property(c => c.AcademicYear).HasMaxLength(20);

        builder.HasIndex(c => c.InstitutionId);
        builder.HasIndex(c => c.IsDeleted);

        builder.HasIndex(c => new { c.Name, c.InstitutionId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasAlternateKey(c => new { c.Id, c.InstitutionId });
    }
}
