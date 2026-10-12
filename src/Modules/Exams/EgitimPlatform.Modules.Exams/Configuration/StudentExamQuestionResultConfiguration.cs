using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public class StudentExamQuestionResultConfiguration : IEntityTypeConfiguration<StudentExamQuestionResult>
{
    public void Configure(EntityTypeBuilder<StudentExamQuestionResult> builder)
    {
        builder.ToTable("StudentExamQuestionResults");

        builder.Property(x => x.SelectedOption).HasMaxLength(10);
        builder.Property(x => x.NetContribution).HasPrecision(5, 2);

        // Prevent duplicate question result for the same attempt and exam question
        builder.HasIndex(x => new { x.StudentExamAttemptId, x.ExamQuestionId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Indexes for performance
        builder.HasIndex(x => new { x.InstitutionId, x.StudentExamAttemptId });
    }
}
