using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Teachers.Configuration;

public class ClassroomStudentConfiguration : IEntityTypeConfiguration<ClassroomStudent>
{
    public void Configure(EntityTypeBuilder<ClassroomStudent> builder)
    {
        builder.ToTable("ClassroomStudents");

        builder.HasIndex(cs => cs.InstitutionId);
        builder.HasIndex(cs => cs.IsDeleted);

        builder.HasOne<Classroom>()
            .WithMany()
            .HasForeignKey(cs => new { cs.ClassroomId, cs.InstitutionId })
            .HasPrincipalKey(c => new { c.Id, c.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cs => new { cs.ClassroomId, cs.StudentId, cs.InstitutionId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
