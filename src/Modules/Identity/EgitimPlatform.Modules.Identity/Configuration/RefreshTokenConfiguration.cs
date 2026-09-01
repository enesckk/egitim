using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Identity.Configuration;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        // TokenHash — SHA-256 produces 64 hex chars
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.RevocationReason).HasMaxLength(100);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.FamilyId);
        builder.HasIndex(t => t.ExpiresAt);

        // Filtered index: active (non-revoked) tokens
        builder.HasIndex(t => new { t.UserId, t.TokenHash })
               .HasFilter("[RevokedAt] IS NULL");

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Concurrency token
        builder.Property(t => t.RowVersion).IsRowVersion();

        // Computed — not mapped
        builder.Ignore(t => t.IsRevoked);
        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsActive);
    }
}
