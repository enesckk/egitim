using EgitimPlatform.Modules.Coaching.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Coaching.Configuration;

public class CoachPrivateNoteConfiguration : IEntityTypeConfiguration<CoachPrivateNote>
{
    public void Configure(EntityTypeBuilder<CoachPrivateNote> builder)
    {
        builder.ToTable("CoachPrivateNotes");

        builder.Property(c => c.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Content)
            .IsRequired();

        builder.HasIndex(c => new { c.InstitutionId, c.StudentId, c.CoachId, c.IsDeleted });
        builder.HasIndex(c => c.StudentId);
        builder.HasIndex(c => c.CoachId);
        builder.HasIndex(c => c.IsDeleted);

        builder.HasOne<Coach>()
            .WithMany()
            .HasForeignKey(c => new { c.CoachId, c.InstitutionId })
            .HasPrincipalKey(c => new { c.Id, c.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
