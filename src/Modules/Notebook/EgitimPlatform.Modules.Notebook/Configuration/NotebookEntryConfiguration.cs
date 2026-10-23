using EgitimPlatform.Modules.Notebook.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Notebook.Configuration;

public class NotebookEntryConfiguration : IEntityTypeConfiguration<NotebookEntry>
{
    public void Configure(EntityTypeBuilder<NotebookEntry> builder)
    {
        builder.ToTable("NotebookEntries");

        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Content)
            .IsRequired();

        builder.Property(n => n.Tags)
            .HasMaxLength(500);

        builder.HasIndex(n => new { n.InstitutionId, n.StudentId, n.IsDeleted });
        builder.HasIndex(n => new { n.StudentId, n.SubjectId });
        builder.HasIndex(n => n.IsPinned);
        builder.HasIndex(n => n.IsDeleted);
    }
}
