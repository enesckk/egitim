using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Exams.Configuration;

public class ExamResultImportConfiguration : IEntityTypeConfiguration<ExamResultImport>
{
    public void Configure(EntityTypeBuilder<ExamResultImport> builder)
    {
        builder.ToTable("ExamResultImports");

        builder.Property(x => x.FileHash).HasMaxLength(100).IsRequired();

        // Unique index preventing duplicate imports of the same workbook file hash per institution, exam, and import type
        builder.HasIndex(x => new { x.InstitutionId, x.ExamId, x.ImportType, x.FileHash })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.InstitutionId, x.ExamId, x.ImportedAt });
    }
}
