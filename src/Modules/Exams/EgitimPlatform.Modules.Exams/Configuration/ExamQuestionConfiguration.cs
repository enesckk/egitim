using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable("ExamQuestions");

        builder.Property(x => x.CorrectOption).HasMaxLength(10).IsRequired();

        // Prevent duplicate question number within the same exam section
        builder.HasIndex(x => new { x.ExamSectionId, x.QuestionNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Prevent adding the same question twice to the same exam section
        builder.HasIndex(x => new { x.ExamSectionId, x.QuestionId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Indexes for performance
        builder.HasIndex(x => new { x.InstitutionId, x.ExamId });
        builder.HasIndex(x => new { x.InstitutionId, x.QuestionId });
    }
}
