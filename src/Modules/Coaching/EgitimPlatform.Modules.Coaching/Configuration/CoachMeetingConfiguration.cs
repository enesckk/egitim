using EgitimPlatform.Modules.Coaching.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Coaching.Configuration;

public class CoachMeetingConfiguration : IEntityTypeConfiguration<CoachMeeting>
{
    public void Configure(EntityTypeBuilder<CoachMeeting> builder)
    {
        builder.ToTable("CoachMeetings");

        builder.Property(m => m.MeetingType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.LocationType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(m => new { m.InstitutionId, m.CoachId, m.StartTime, m.IsDeleted });
        builder.HasIndex(m => new { m.InstitutionId, m.StudentId, m.StartTime, m.IsDeleted });
        builder.HasIndex(m => m.IsDeleted);

        builder.HasOne<Coach>()
            .WithMany()
            .HasForeignKey(m => new { m.CoachId, m.InstitutionId })
            .HasPrincipalKey(c => new { c.Id, c.InstitutionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
