using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Planning.Configuration;

public class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable("StudyPlans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(p => p.RecommendationPolicyVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.StudyPlanPolicyVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.UtilizationRate)
            .HasPrecision(7, 4);

        builder.HasIndex(p => p.InstitutionId);

        builder.HasIndex(p => new { p.StudentId, p.Status });

        // Filtered unique index: at most 1 active plan per student
        builder.HasIndex(p => p.StudentId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0");

        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.StudyPlan)
            .HasForeignKey(t => t.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
