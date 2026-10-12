using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Planning.Configuration;

public class CoachTaskConfiguration : IEntityTypeConfiguration<CoachTask>
{
    public void Configure(EntityTypeBuilder<CoachTask> builder)
    {
        builder.ToTable("CoachTasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.SubjectName)
            .HasMaxLength(200);

        builder.Property(t => t.TopicName)
            .HasMaxLength(200);

        builder.Property(t => t.LearningOutcomeName)
            .HasMaxLength(200);

        builder.HasIndex(t => new { t.StudentId, t.Status });
        builder.HasIndex(t => new { t.StudentId, t.Status, t.DueDate });
        builder.HasIndex(t => new { t.CoachUserId, t.Status });
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.InstitutionId);
    }
}
