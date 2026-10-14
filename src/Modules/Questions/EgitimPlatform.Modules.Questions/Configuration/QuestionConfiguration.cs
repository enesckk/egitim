using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Questions.Configuration;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.MediaUrl).HasMaxLength(1000);

        // Performance Indexes
        builder.HasIndex(x => new { x.InstitutionId, x.SubjectId, x.TopicId });
        builder.HasIndex(x => new { x.SubjectId, x.Difficulty });
        builder.HasIndex(x => new { x.SourceType, x.SourceYear });
        builder.HasIndex(x => new { x.InstitutionId, x.Status });

        // Relationships
        builder.HasMany(x => x.Options)
            .WithOne()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StyleAssignments)
            .WithOne(x => x.Question)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
