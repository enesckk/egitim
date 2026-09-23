using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public class StudentExamAttemptConfiguration : IEntityTypeConfiguration<StudentExamAttempt>
{
    public void Configure(EntityTypeBuilder<StudentExamAttempt> builder)
    {
        builder.ToTable("StudentExamAttempts");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Property(x => x.TotalNet).HasPrecision(7, 2);
        builder.Property(x => x.ReportedScore).HasPrecision(7, 2);

        // Indexes
        builder.HasIndex(x => new { x.InstitutionId, x.StudentId, x.TakenAt, x.Id });
        builder.HasIndex(x => new { x.InstitutionId, x.ExamId, x.Status });

        // Filtered unique active index on (ExamId, StudentId, AttemptNumber)
        builder.HasIndex(x => new { x.ExamId, x.StudentId, x.AttemptNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relationships
        builder.HasMany(x => x.SubjectResults)
            .WithOne()
            .HasForeignKey(x => x.StudentExamAttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.TopicResults)
            .WithOne()
            .HasForeignKey(x => x.StudentExamAttemptId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
