using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Students.Configuration;

public class StudentParentConfiguration : IEntityTypeConfiguration<StudentParent>
{
    public void Configure(EntityTypeBuilder<StudentParent> builder)
    {
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.ToTable("StudentParents");

        builder.Property(sp => sp.RelationshipType).HasMaxLength(50).IsRequired();
        builder.Property(sp => sp.Notes).HasMaxLength(500);

        builder.HasIndex(sp => sp.InstitutionId);
        builder.HasIndex(sp => new { sp.StudentId, sp.IsActive });
        builder.HasIndex(sp => new { sp.ParentId, sp.IsActive });

        // Composite FKs ensure same-institution constraint at DB level
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(sp => new { sp.StudentId, sp.InstitutionId })
            .HasPrincipalKey(s => new { s.Id, s.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Parent>()
            .WithMany()
            .HasForeignKey(sp => new { sp.ParentId, sp.InstitutionId })
            .HasPrincipalKey(p => new { p.Id, p.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one active relationship per student-parent pair
        builder.HasIndex(sp => new { sp.StudentId, sp.ParentId })
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
    }
}
