using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public class StudentExamTopicResultConfiguration : IEntityTypeConfiguration<StudentExamTopicResult>
{
    public void Configure(EntityTypeBuilder<StudentExamTopicResult> builder)
    {
        builder.ToTable("StudentExamTopicResults");
        builder.Property(x => x.Net).HasPrecision(7, 2);

        // Unique index per attempt, section & topic
        builder.HasIndex(x => new { x.StudentExamAttemptId, x.ExamSectionId, x.TopicId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Index on (TopicId, StudentExamAttemptId)
        builder.HasIndex(x => new { x.TopicId, x.StudentExamAttemptId });
    }
}
