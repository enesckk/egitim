using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Students.Configuration;

public class StudentGoalHistoryConfiguration : IEntityTypeConfiguration<StudentGoalHistory>
{
    public void Configure(EntityTypeBuilder<StudentGoalHistory> builder)
    {
        builder.ToTable("StudentGoalHistories");

        builder.Property(h => h.Action).HasMaxLength(50).IsRequired();
        builder.Property(h => h.PreviousValuesJson).HasMaxLength(4000);
        builder.Property(h => h.NewValuesJson).HasMaxLength(4000);
        builder.Property(h => h.CorrelationId).HasMaxLength(100);

        // Indexes
        builder.HasIndex(h => h.StudentGoalId);
        builder.HasIndex(h => h.ChangedAt);

        // FK to StudentGoal — Restrict so history is never orphaned by goal deletion.
        builder.HasOne<StudentGoal>()
            .WithMany()
            .HasForeignKey(h => h.StudentGoalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
