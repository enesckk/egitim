using EgitimPlatform.Modules.Coaching.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Coaching.Configuration;

public class StudentCoachAssignmentConfiguration : IEntityTypeConfiguration<StudentCoachAssignment>
{
    public void Configure(EntityTypeBuilder<StudentCoachAssignment> builder)
    {
        builder.ToTable("StudentCoachAssignments");

        builder.HasIndex(a => new { a.StudentId, a.CoachId, a.AssignedAt }).IsUnique();
        builder.HasIndex(a => a.InstitutionId);
        builder.HasIndex(a => new { a.StudentId, a.IsActive });
        builder.HasIndex(a => new { a.CoachId, a.IsActive });

        // Student and Coach are external references — no FK navigation here
        // Cross-institution constraints enforced at application level
    }
}
