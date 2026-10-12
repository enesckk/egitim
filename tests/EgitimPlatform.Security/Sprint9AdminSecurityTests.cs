using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Sprint9AdminSecurityTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task InstitutionAdmin_OwnTenant_Allowed()
    {
        using var db = Db();
        var adminUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(adminUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.InstitutionAdmin).Returns(true);

        db.Set<Student>().Add(new Student
        {
            Id = studentId,
            InstitutionId = instId,
            FirstName = "Ali",
            LastName = "Yilmaz",
            StudentNumber = "101",
            GradeLevel = 10,
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.Set<Coach>().Add(new Coach
        {
            Id = coachId,
            InstitutionId = instId,
            UserId = Guid.NewGuid(),
            FirstName = "Ayse",
            LastName = "Koc",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new AssignCoachHandler(db, currentUser, Substitute.For<IAuditService>());
        await handler.HandleAsync(new AssignCoachCommand(studentId, coachId, true));

        var assignment = await db.Set<StudentCoachAssignment>()
            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.CoachId == coachId);
        Assert.NotNull(assignment);
        Assert.True(assignment.IsActive);
    }

    [Fact]
    public async Task InstitutionAdmin_CrossTenant_Denied()
    {
        using var db = Db();
        var adminUserId = Guid.NewGuid();
        var adminInstId = Guid.NewGuid();
        var foreignInstId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(adminUserId);
        currentUser.GetInstitutionIdAsync().Returns(adminInstId);
        currentUser.IsInRole(Roles.InstitutionAdmin).Returns(true);

        db.Set<Student>().Add(new Student
        {
            Id = studentId,
            InstitutionId = foreignInstId, // Foreign tenant
            FirstName = "Veli",
            LastName = "Demir",
            StudentNumber = "102",
            GradeLevel = 11,
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.Set<Coach>().Add(new Coach
        {
            Id = coachId,
            InstitutionId = adminInstId,
            UserId = Guid.NewGuid(),
            FirstName = "Ayse",
            LastName = "Koc",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new AssignCoachHandler(db, currentUser, Substitute.For<IAuditService>());
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(new AssignCoachCommand(studentId, coachId, true)));
    }

    [Fact]
    public async Task NonAdminRoles_CoachAssignmentMutation_Denied()
    {
        using var db = Db();
        var instId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        var rolesToTest = new[] { Roles.Coach, Roles.Teacher, Roles.Parent, Roles.Student };

        foreach (var role in rolesToTest)
        {
            var currentUser = Substitute.For<ICurrentUser>();
            currentUser.UserId.Returns(Guid.NewGuid());
            currentUser.GetInstitutionIdAsync().Returns(instId);
            currentUser.IsInRole(role).Returns(true);
            currentUser.IsInRole(Roles.InstitutionAdmin).Returns(false);
            currentUser.IsSuperAdmin.Returns(false);

            var handler = new AssignCoachHandler(db, currentUser, Substitute.For<IAuditService>());
            await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(new AssignCoachCommand(studentId, coachId, true)));
        }
    }
}
