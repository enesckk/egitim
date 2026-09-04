using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Students.Configuration;

public class StudentGoalConfiguration : IEntityTypeConfiguration<StudentGoal>
{
    public void Configure(EntityTypeBuilder<StudentGoal> builder)
    {
        builder.ToTable("StudentGoals");

        builder.Property(g => g.Title).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(2000);
        builder.Property(g => g.TargetSchoolName).HasMaxLength(200);

        // Indexes
        builder.HasIndex(g => g.InstitutionId);
        builder.HasIndex(g => new { g.StudentId, g.IsActive });
        builder.HasIndex(g => g.IsDeleted);
        builder.HasIndex(g => new { g.InstitutionId, g.IsDeleted });

        // Composite FK to Student via (Id, InstitutionId) alternate key — same-tenant enforcement.
        // Sprint 2: Student→Institution FK already exists, so this transitively enforces same institution.
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(g => new { g.StudentId, g.InstitutionId })
            .HasPrincipalKey(s => new { s.Id, s.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        // TargetExamTypeId FK — will be wired when Academic module has ExamType entity.
        // For now it's a nullable Guid column with no FK constraint.
    }
}
