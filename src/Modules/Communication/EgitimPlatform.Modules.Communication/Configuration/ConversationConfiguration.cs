using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Communication.Configuration;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Subject)
            .HasMaxLength(200);

        builder.Property(c => c.LastMessageSnippet)
            .HasMaxLength(500);

        builder.HasIndex(c => c.InstitutionId);
        builder.HasIndex(c => new { c.InstitutionId, c.Participant1Id, c.Participant2Id, c.IsDeleted })
            .IsUnique();
        builder.HasIndex(c => new { c.InstitutionId, c.Participant1Id, c.LastMessageAt, c.IsDeleted });
        builder.HasIndex(c => new { c.InstitutionId, c.Participant2Id, c.LastMessageAt, c.IsDeleted });

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
