using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exams", table =>
            table.HasCheckConstraint("CK_Exams_WrongAnswerPenalty", "[WrongAnswerPenalty] > 0"));

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.WrongAnswerPenalty).HasPrecision(9, 4).HasDefaultValue(4m);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasAlternateKey(x => new { x.Id, x.InstitutionId });
        builder.HasIndex(x => new { x.InstitutionId, x.ExamDate, x.Id });
        builder.HasIndex(x => new { x.InstitutionId, x.ExamTypeId, x.ExamDate });
        builder.HasIndex(x => new { x.InstitutionId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.InstitutionId, x.IsDeleted });
    }
}
