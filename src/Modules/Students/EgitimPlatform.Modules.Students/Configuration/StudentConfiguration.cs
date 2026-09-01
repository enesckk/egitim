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

        builder.HasIndex(s => s.InstitutionId);
        builder.HasIndex(s => s.UserId).HasFilter("[UserId] IS NOT NULL");
        builder.HasIndex(s => new { s.LastName, s.FirstName });
        builder.HasIndex(s => s.IsDeleted);
        builder.HasIndex(s => new { s.InstitutionId, s.IsDeleted });
    }
}
