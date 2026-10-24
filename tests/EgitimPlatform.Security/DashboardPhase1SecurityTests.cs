using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features.GetInstitutionOverview;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using EgitimPlatform.Modules.Teachers.Features.GetTeacherDashboard;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class DashboardPhase1SecurityTests
{
    private ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetTeacherDashboard_UnauthorizedRole_ThrowsForbiddenException()
    {
        using var db = CreateDb();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.GetInstitutionIdAsync().Returns(Guid.NewGuid());
        currentUser.IsInRole(Roles.Teacher).Returns(false);

        var handler = new GetTeacherDashboardHandler(db, currentUser, Substitute.For<IAcademicCatalog>());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetTeacherDashboard_TeacherNotFound_ThrowsForbiddenException()
    {
        using var db = CreateDb();
        var currentUser = Substitute.For<ICurrentUser>();
        var userId = Guid.NewGuid();
        currentUser.UserId.Returns(userId);
        currentUser.GetInstitutionIdAsync().Returns(Guid.NewGuid());
        currentUser.IsInRole(Roles.Teacher).Returns(true);

        var handler = new GetTeacherDashboardHandler(db, currentUser, Substitute.For<IAcademicCatalog>());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetTeacherDashboard_OnlyReturnsAssignedClassroomStudents()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Teacher).Returns(true);

        // Teacher
        db.Set<Teacher>().Add(new Teacher
        {
            Id = teacherId,
            UserId = userId,
            InstitutionId = instId,
            FirstName = "Ahmet",
            LastName = "Hoca",
            CreatedAt = DateTimeOffset.UtcNow
        });

        // Classrooms
        var classAssigned = Guid.NewGuid();
        var classOther = Guid.NewGuid();

        db.Set<ClassroomTeacher>().Add(new ClassroomTeacher
        {
            ClassroomId = classAssigned,
            TeacherId = teacherId,
            InstitutionId = instId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        // Students: 1 assigned to classAssigned, 1 to classOther
        var studentAssigned = Guid.NewGuid();
        var studentOther = Guid.NewGuid();

        db.Set<Student>().AddRange(
            new Student { Id = studentAssigned, InstitutionId = instId, FirstName = "Can", LastName = "Test", StudentNumber = "101", CreatedAt = DateTimeOffset.UtcNow },
            new Student { Id = studentOther, InstitutionId = instId, FirstName = "Gizli", LastName = "Ogrenci", StudentNumber = "102", CreatedAt = DateTimeOffset.UtcNow }
        );

        db.Set<ClassroomStudent>().AddRange(
            new ClassroomStudent { ClassroomId = classAssigned, StudentId = studentAssigned, InstitutionId = instId, CreatedAt = DateTimeOffset.UtcNow },
            new ClassroomStudent { ClassroomId = classOther, StudentId = studentOther, InstitutionId = instId, CreatedAt = DateTimeOffset.UtcNow }
        );

        await db.SaveChangesAsync();

        var handler = new GetTeacherDashboardHandler(db, currentUser, Substitute.For<IAcademicCatalog>());
        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.Equal("Ahmet Hoca", result.TeacherName);
        Assert.Equal(1, result.TotalAssignedClasses);
        Assert.Equal(1, result.TotalAssignedStudents);
        Assert.DoesNotContain(result.StudentNets, s => s.StudentId == studentOther.ToString());
    }

    [Fact]
    public async Task GetInstitutionOverview_UnauthorizedRole_ThrowsForbiddenException()
    {
        using var db = CreateDb();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.GetInstitutionIdAsync().Returns(Guid.NewGuid());
        currentUser.IsInRole(Roles.InstitutionAdmin).Returns(false);
        currentUser.IsInRole(Roles.SuperAdmin).Returns(false);

        var handler = new GetInstitutionOverviewHandler(db, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetInstitutionOverview_StrictTenantIsolation_CalculatesOwnTenantOnly()
    {
        using var db = CreateDb();
        var inst1 = Guid.NewGuid();
        var inst2 = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.GetInstitutionIdAsync().Returns(inst1);
        currentUser.IsInRole(Roles.InstitutionAdmin).Returns(true);

        // Add Active StudyPlans: 1 in inst1, 1 in inst2
        db.Set<StudyPlan>().AddRange(
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = inst1,
                StudentId = Guid.NewGuid(),
                Status = StudyPlanStatus.Active,
                UtilizationRate = 0.85m,
                TotalPlannedMinutes = 1200,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = inst2,
                StudentId = Guid.NewGuid(),
                Status = StudyPlanStatus.Active,
                UtilizationRate = 0.20m,
                TotalPlannedMinutes = 300,
                CreatedAt = DateTimeOffset.UtcNow
            }
        );

        await db.SaveChangesAsync();

        var handler = new GetInstitutionOverviewHandler(db, currentUser);
        var result = await handler.HandleAsync(CancellationToken.None);

        // Adherence should be 85% for inst1, not polluted by inst2's 20%
        var adherenceKpi = result.Kpis.First(k => k.Label.Contains("Plan Uyumu"));
        Assert.Equal("%85", adherenceKpi.Value);
    }
}
