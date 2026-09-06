using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Teachers.Configuration;

public class TeacherSubjectConfiguration : IEntityTypeConfiguration<TeacherSubject>
{
    public void Configure(EntityTypeBuilder<TeacherSubject> builder)
    {
        builder.ToTable("TeacherSubjects");


        builder.HasIndex(ts => ts.InstitutionId);

        // Composite FK to Teacher (same-institution enforcement)
        builder.HasOne<Teacher>()
            .WithMany()
            .HasForeignKey(ts => new { ts.TeacherId, ts.InstitutionId })
            .HasPrincipalKey(t => new { t.Id, t.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        // Unique: teacher cannot have duplicate subject scope in same institution
        builder.HasIndex(ts => new { ts.TeacherId, ts.SubjectId, ts.InstitutionId })
            .IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
