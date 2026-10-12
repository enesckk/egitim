using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public sealed class StudentExamAttemptConfiguration : IEntityTypeConfiguration<StudentExamAttempt>
{
    public void Configure(EntityTypeBuilder<StudentExamAttempt> builder)
    {
        builder.ToTable("StudentExamAttempts", table =>
        {
            table.HasCheckConstraint("CK_StudentExamAttempts_AttemptNumber", "[AttemptNumber] >= 1");
            table.HasCheckConstraint("CK_StudentExamAttempts_Totals", "[TotalCorrect] >= 0 AND [TotalWrong] >= 0 AND [TotalBlank] >= 0");
        });

        builder.Property(x => x.TotalNet).HasPrecision(18, 4);
        builder.Property(x => x.ReportedScore).HasPrecision(12, 4);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasAlternateKey(x => new { x.Id, x.InstitutionId, x.ExamId });

        builder.HasOne<Exam>()
            .WithMany()
            .HasForeignKey(x => new { x.ExamId, x.InstitutionId })
            .HasPrincipalKey(x => new { x.Id, x.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.InstitutionId, x.StudentId, x.TakenAt, x.Id });
        builder.HasIndex(x => new { x.InstitutionId, x.ExamId, x.Status });
        builder.HasIndex(x => new { x.ExamId, x.StudentId, x.AttemptNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.InstitutionId, x.IsDeleted });
    }
}
