using EgitimPlatform.Modules.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EgitimPlatform.Modules.Academic.Configuration;
public class LearningOutcomeConfiguration : IEntityTypeConfiguration<LearningOutcome>
{
    public void Configure(EntityTypeBuilder<LearningOutcome> b)
    {
        b.ToTable("AcademicLearningOutcomes");
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        // Codes are never reused, even after soft deletion.
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne<SubTopic>().WithMany().HasForeignKey(x => x.SubTopicId).OnDelete(DeleteBehavior.Restrict);
    }
}
