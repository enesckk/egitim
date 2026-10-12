using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Questions.Configuration;

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("QuestionOptions");

        builder.Property(x => x.OptionLabel).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Content).IsRequired();

        // Unique constraint per question option label
        builder.HasIndex(x => new { x.QuestionId, x.OptionLabel })
            .IsUnique();
    }
}
