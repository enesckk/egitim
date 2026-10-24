using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Communication.Configuration;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.HasIndex(m => m.InstitutionId);
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt, m.IsDeleted });
        builder.HasIndex(m => new { m.RecipientUserId, m.IsRead, m.IsDeleted });
    }
}
