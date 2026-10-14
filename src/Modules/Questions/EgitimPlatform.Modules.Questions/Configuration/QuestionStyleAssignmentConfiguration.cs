using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Questions.Configuration;

public class QuestionStyleAssignmentConfiguration : IEntityTypeConfiguration<QuestionStyleAssignment>
{
    public void Configure(EntityTypeBuilder<QuestionStyleAssignment> builder)
    {
        builder.ToTable("QuestionStyleAssignments");
        builder.HasKey(x => new { x.QuestionId, x.QuestionStyleId });

        builder.HasOne(x => x.Question)
            .WithMany(x => x.StyleAssignments)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.QuestionStyle)
            .WithMany(x => x.QuestionAssignments)
            .HasForeignKey(x => x.QuestionStyleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
