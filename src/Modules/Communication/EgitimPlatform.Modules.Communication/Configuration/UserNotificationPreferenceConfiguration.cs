using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Communication.Configuration;

public class UserNotificationPreferenceConfiguration : IEntityTypeConfiguration<UserNotificationPreference>
{
    public void Configure(EntityTypeBuilder<UserNotificationPreference> builder)
    {
        builder.ToTable("UserNotificationPreferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.InstitutionId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();

        builder.HasIndex(p => new { p.InstitutionId, p.UserId }).IsUnique();
    }
}
