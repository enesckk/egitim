using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Coaching.Features.ManageMeetings;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class CoachMeetingsPhase2SecurityTests
{
    private ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateMeeting_UnauthorizedRole_ThrowsForbiddenException()
    {
        using var db = CreateDb();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.GetInstitutionIdAsync().Returns(Guid.NewGuid());
        currentUser.IsInRole(Roles.Coach).Returns(false);
        currentUser.IsSuperAdmin.Returns(false);

        var handler = new CoachMeetingsHandler(db, currentUser, Substitute.For<IAuditService>());

        var req = new CreateCoachMeetingRequest(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(2),
            30,
            "Haftalık Takip",
            "Online Video",
            "Not"
        );

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.CreateAsync(req));
    }

    [Fact]
    public async Task CreateMeeting_UnassignedStudent_ThrowsForbiddenException()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        db.Set<Coach>().Add(new Coach
        {
            Id = coachId,
            UserId = userId,
            InstitutionId = instId,
            FirstName = "Ali",
            LastName = "Koç",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new CoachMeetingsHandler(db, currentUser, Substitute.For<IAuditService>());

        var req = new CreateCoachMeetingRequest(
            studentId,
            DateTimeOffset.UtcNow.AddHours(2),
            30,
            "Haftalık Takip",
            "Online Video",
            "Not"
        );

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.CreateAsync(req));
    }

    [Fact]
    public async Task CreateMeeting_RevokedAssignment_ThrowsForbiddenException()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        db.Set<Coach>().Add(new Coach
        {
            Id = coachId,
            UserId = userId,
            InstitutionId = instId,
            FirstName = "Ali",
            LastName = "Koç",
            CreatedAt = DateTimeOffset.UtcNow
        });

        // Revoked assignment (IsActive = false, EndedAt set)
        db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
        {
            Id = Guid.NewGuid(),
            CoachId = coachId,
            StudentId = studentId,
            InstitutionId = instId,
            IsActive = false,
            EndedAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
        });

        await db.SaveChangesAsync();

        var handler = new CoachMeetingsHandler(db, currentUser, Substitute.For<IAuditService>());

        var req = new CreateCoachMeetingRequest(
            studentId,
            DateTimeOffset.UtcNow.AddHours(2),
            30,
            "Haftalık Takip",
            "Online Video",
            "Not"
        );

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.CreateAsync(req));
    }

    [Fact]
    public async Task CreateMeeting_OverlappingTimeslot_ThrowsConflictException()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        db.Set<Coach>().Add(new Coach
        {
            Id = coachId,
            UserId = userId,
            InstitutionId = instId,
            FirstName = "Ali",
            LastName = "Koç",
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
        {
            Id = Guid.NewGuid(),
            CoachId = coachId,
            StudentId = studentId,
            InstitutionId = instId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var baseTime = DateTimeOffset.UtcNow.AddHours(2);

        db.Set<CoachMeeting>().Add(new CoachMeeting
        {
            Id = Guid.NewGuid(),
            CoachId = coachId,
            StudentId = studentId,
            InstitutionId = instId,
            StartTime = baseTime,
            EndTime = baseTime.AddMinutes(30),
            DurationMinutes = 30,
            MeetingType = "Seans 1",
            LocationType = "Online Video",
            Status = CoachMeetingStatus.Upcoming,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new CoachMeetingsHandler(db, currentUser, Substitute.For<IAuditService>());

        // Attempt overlapping meeting (starts 15 mins into existing 30 min meeting)
        var req = new CreateCoachMeetingRequest(
            studentId,
            baseTime.AddMinutes(15),
            30,
            "Seans 2",
            "Online Video",
            "Çakışma testi"
        );

        await Assert.ThrowsAsync<ConflictException>(() => handler.CreateAsync(req));
    }

    [Fact]
    public async Task CreateMeeting_ValidActiveStudent_PersistsSuccessfully()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        db.Set<Coach>().Add(new Coach
        {
            Id = coachId,
            UserId = userId,
            InstitutionId = instId,
            FirstName = "Ali",
            LastName = "Koç",
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
        {
            Id = Guid.NewGuid(),
            CoachId = coachId,
            StudentId = studentId,
            InstitutionId = instId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        var handler = new CoachMeetingsHandler(db, currentUser, Substitute.For<IAuditService>());

        var startTime = DateTimeOffset.UtcNow.AddHours(5);
        var req = new CreateCoachMeetingRequest(
            studentId,
            startTime,
            45,
            "Motivasyon Seansı",
            "Yüz Yüze • Kurum",
            "Haftalık kontrol"
        );

        var created = await handler.CreateAsync(req);

        Assert.Equal(studentId, created.StudentId);
        Assert.Equal(45, created.DurationMinutes);
        Assert.Equal("Motivasyon Seansı", created.Type);
        Assert.Equal("upcoming", created.Status);

        var inDb = await db.Set<CoachMeeting>().FirstOrDefaultAsync(m => m.Id == created.Id);
        Assert.NotNull(inDb);
        Assert.Equal(instId, inDb.InstitutionId);
        Assert.Equal(coachId, inDb.CoachId);
    }
}
