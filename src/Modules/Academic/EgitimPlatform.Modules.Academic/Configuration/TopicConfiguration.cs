using EgitimPlatform.Modules.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EgitimPlatform.Modules.Academic.Configuration;
public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> b)
    {
        b.ToTable("AcademicTopics");
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        // Codes are never reused, even after soft deletion.
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne<Subject>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
    }
}
