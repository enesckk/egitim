using EgitimPlatform.Modules.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EgitimPlatform.Modules.Academic.Configuration;
public class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    public void Configure(EntityTypeBuilder<Concept> b)
    {
        b.ToTable("AcademicConcepts");
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        // Codes are never reused, even after soft deletion.
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne<LearningOutcome>().WithMany().HasForeignKey(x => x.LearningOutcomeId).OnDelete(DeleteBehavior.Restrict);
    }
}
