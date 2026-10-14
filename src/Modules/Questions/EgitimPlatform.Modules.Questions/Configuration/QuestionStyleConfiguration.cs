using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Questions.Configuration;

public class QuestionStyleConfiguration : IEntityTypeConfiguration<QuestionStyle>
{
    public void Configure(EntityTypeBuilder<QuestionStyle> builder)
    {
        builder.ToTable("QuestionStyles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.DisplayOrder, x.Name });
    }
}
