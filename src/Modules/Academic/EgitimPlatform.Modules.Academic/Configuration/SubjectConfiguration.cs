using EgitimPlatform.Modules.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EgitimPlatform.Modules.Academic.Configuration;
public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("AcademicSubjects");
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        // Codes are never reused, even after soft deletion.
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne<ExamType>().WithMany().HasForeignKey(x => x.ExamTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
