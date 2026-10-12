using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Planning.Configuration;

public class StudyTaskConfiguration : IEntityTypeConfiguration<StudyTask>
{
    public void Configure(EntityTypeBuilder<StudyTask> builder)
    {
        builder.ToTable("StudyTasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TitleSnapshot)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(t => t.SubjectName)
            .HasMaxLength(200);

        builder.Property(t => t.TopicName)
            .HasMaxLength(200);

        builder.Property(t => t.SubTopicName)
            .HasMaxLength(200);

        builder.Property(t => t.LearningOutcomeName)
            .HasMaxLength(300);

        builder.HasIndex(t => t.StudyPlanId);

        builder.HasIndex(t => t.InstitutionId);

        builder.HasIndex(t => new { t.StudentId, t.PlannedDate });

        builder.HasIndex(t => new { t.StudentId, t.IsCompleted, t.PlannedDate });
    }
}
