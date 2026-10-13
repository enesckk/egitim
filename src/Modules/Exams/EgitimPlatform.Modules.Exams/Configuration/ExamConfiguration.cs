using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exams");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.WrongAnswerPenalty).HasPrecision(5, 2);

        // Filtered unique active index on (InstitutionId, Code)
        builder.HasIndex(x => new { x.InstitutionId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Indexes for performance
        builder.HasIndex(x => new { x.InstitutionId, x.ExamDate, x.Id });
        builder.HasIndex(x => new { x.InstitutionId, x.ExamTypeId, x.ExamDate });

        // Relationships
        builder.HasMany(x => x.Sections)
            .WithOne()
            .HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
