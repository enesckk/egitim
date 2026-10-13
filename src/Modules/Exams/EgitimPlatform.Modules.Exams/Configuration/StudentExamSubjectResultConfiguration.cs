using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public class StudentExamSubjectResultConfiguration : IEntityTypeConfiguration<StudentExamSubjectResult>
{
    public void Configure(EntityTypeBuilder<StudentExamSubjectResult> builder)
    {
        builder.ToTable("StudentExamSubjectResults");
        builder.Property(x => x.Net).HasPrecision(7, 2);

        // Unique index per attempt & section
        builder.HasIndex(x => new { x.StudentExamAttemptId, x.ExamSectionId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
