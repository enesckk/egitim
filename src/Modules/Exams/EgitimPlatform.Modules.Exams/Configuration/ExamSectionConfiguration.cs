using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public class ExamSectionConfiguration : IEntityTypeConfiguration<ExamSection>
{
    public void Configure(EntityTypeBuilder<ExamSection> builder)
    {
        builder.ToTable("ExamSections");
        builder.Property(x => x.WrongAnswerPenalty).HasPrecision(5, 2);

        // Filtered unique active index on (ExamId, SubjectId)
        builder.HasIndex(x => new { x.ExamId, x.SubjectId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Index on (ExamId, DisplayOrder)
        builder.HasIndex(x => new { x.ExamId, x.DisplayOrder });
    }
}
