using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Teachers.Configuration;

public class ClassroomTeacherConfiguration : IEntityTypeConfiguration<ClassroomTeacher>
{
    public void Configure(EntityTypeBuilder<ClassroomTeacher> builder)
    {
        builder.ToTable("ClassroomTeachers");

        builder.HasIndex(ct => ct.InstitutionId);
        builder.HasIndex(ct => ct.IsDeleted);

        builder.HasOne<Classroom>()
            .WithMany()
            .HasForeignKey(ct => new { ct.ClassroomId, ct.InstitutionId })
            .HasPrincipalKey(c => new { c.Id, c.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Teacher>()
            .WithMany()
            .HasForeignKey(ct => new { ct.TeacherId, ct.InstitutionId })
            .HasPrincipalKey(t => new { t.Id, t.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ct => new { ct.ClassroomId, ct.TeacherId, ct.InstitutionId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
