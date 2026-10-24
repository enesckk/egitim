using EgitimPlatform.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Content.Configuration;

public class ContentMaterialAssignmentConfiguration : IEntityTypeConfiguration<ContentMaterialAssignment>
{
    public void Configure(EntityTypeBuilder<ContentMaterialAssignment> builder)
    {
        builder.ToTable("ContentMaterialAssignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Note).HasMaxLength(500);

        builder.HasIndex(a => a.InstitutionId);
        builder.HasIndex(a => new { a.InstitutionId, a.ClassroomId, a.IsActive });
        builder.HasIndex(a => new { a.MaterialId, a.ClassroomId }).IsUnique();
    }
}
