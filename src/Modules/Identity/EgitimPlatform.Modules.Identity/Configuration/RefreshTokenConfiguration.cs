using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Identity.Configuration;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.Property(t => t.Token).HasMaxLength(256).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.ExpiresAt);
        builder.HasIndex(t => new { t.UserId, t.RevokedAt });

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.IsRevoked);
        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsActive);
    }
}
