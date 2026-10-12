using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public sealed class ExamSectionConfiguration : IEntityTypeConfiguration<ExamSection>
{
    public void Configure(EntityTypeBuilder<ExamSection> builder)
    {
        builder.ToTable("ExamSections", table =>
        {
            table.HasCheckConstraint("CK_ExamSections_DisplayOrder", "[DisplayOrder] >= 0");
            table.HasCheckConstraint("CK_ExamSections_QuestionCount", "[QuestionCount] > 0");
            table.HasCheckConstraint("CK_ExamSections_WrongAnswerPenalty", "[WrongAnswerPenalty] IS NULL OR [WrongAnswerPenalty] > 0");
        });

        builder.Property(x => x.WrongAnswerPenalty).HasPrecision(9, 4);
        builder.HasAlternateKey(x => new { x.Id, x.InstitutionId, x.ExamId });

        builder.HasOne<Exam>()
            .WithMany()
            .HasForeignKey(x => new { x.ExamId, x.InstitutionId })
            .HasPrincipalKey(x => new { x.Id, x.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ExamId, x.SubjectId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.ExamId, x.DisplayOrder });
        builder.HasIndex(x => new { x.InstitutionId, x.IsDeleted });
    }
}
