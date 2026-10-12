using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public sealed class StudentExamTopicResultConfiguration : IEntityTypeConfiguration<StudentExamTopicResult>
{
    public void Configure(EntityTypeBuilder<StudentExamTopicResult> builder)
    {
        builder.ToTable("StudentExamTopicResults", table =>
            table.HasCheckConstraint("CK_StudentExamTopicResults_Counts", "[Correct] >= 0 AND [Wrong] >= 0 AND [Blank] >= 0"));
        builder.Property(x => x.Net).HasPrecision(18, 4);

        builder.HasOne<StudentExamAttempt>()
            .WithMany()
            .HasForeignKey(x => new { x.StudentExamAttemptId, x.InstitutionId, x.ExamId })
            .HasPrincipalKey(x => new { x.Id, x.InstitutionId, x.ExamId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExamSection>()
            .WithMany()
            .HasForeignKey(x => new { x.ExamSectionId, x.InstitutionId, x.ExamId })
            .HasPrincipalKey(x => new { x.Id, x.InstitutionId, x.ExamId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.StudentExamAttemptId, x.ExamSectionId, x.TopicId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.TopicId, x.StudentExamAttemptId });
        builder.HasIndex(x => new { x.InstitutionId, x.IsDeleted });
    }
}
