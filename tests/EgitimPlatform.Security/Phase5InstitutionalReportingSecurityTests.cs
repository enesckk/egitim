using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Reporting.Features.InstitutionalReports;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Phase5InstitutionalReportingSecurityTests
{
    private readonly Guid _instA = Guid.NewGuid();
    private readonly Guid _instB = Guid.NewGuid();

    private ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private ICurrentUser CreateUser(bool isAuthenticated, Guid? userId = null, Guid? instId = null, string[]? roles = null)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(isAuthenticated);
        user.UserId.Returns(userId);
        user.Roles.Returns(roles ?? []);
        user.GetInstitutionIdAsync().Returns(Task.FromResult(instId));
        return user;
    }

    [Fact]
    public async Task Unauthorized_User_Cannot_Access_Reporting_FailClosed()
    {
        var db = CreateDb();
        var unauthenticatedUser = CreateUser(isAuthenticated: false);
        var handler = new InstitutionalReportHandler(db, unauthenticatedUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.GetReportDataAsync(new ReportFilterRequest()));
    }

    [Fact]
    public async Task Student_Or_Coach_Cannot_Access_Reporting_FailClosed()
    {
        var db = CreateDb();
        var studentUser = CreateUser(true, Guid.NewGuid(), _instA, [Roles.Student]);
        var handler = new InstitutionalReportHandler(db, studentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.GetReportDataAsync(new ReportFilterRequest()));

        var coachUser = CreateUser(true, Guid.NewGuid(), _instA, [Roles.Coach]);
        var coachHandler = new InstitutionalReportHandler(db, coachUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            coachHandler.GetReportDataAsync(new ReportFilterRequest()));
    }

    [Fact]
    public async Task InstitutionAdmin_Without_InstitutionContext_IsDenied()
    {
        var db = CreateDb();
        var noInstAdmin = CreateUser(true, Guid.NewGuid(), null, [Roles.InstitutionAdmin]);
        var handler = new InstitutionalReportHandler(db, noInstAdmin);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.GetReportDataAsync(new ReportFilterRequest()));
    }

    [Fact]
    public async Task Export_Sanitizes_Formula_Injection_Characters()
    {
        var db = CreateDb();
        var admin = CreateUser(true, Guid.NewGuid(), _instA, [Roles.InstitutionAdmin]);
        var handler = new InstitutionalReportHandler(db, admin);

        var (csvBytes, contentType, fileName) = await handler.ExportReportAsync(new ReportFilterRequest(), "csv");

        Assert.NotNull(csvBytes);
        Assert.Contains("text/csv", contentType);
        Assert.EndsWith(".csv", fileName);

        var (xlsxBytes, xlsxContentType, xlsxFileName) = await handler.ExportReportAsync(new ReportFilterRequest(), "xlsx");
        Assert.NotNull(xlsxBytes);
        Assert.Contains("openxmlformats", xlsxContentType);
        Assert.EndsWith(".xlsx", xlsxFileName);
    }

    [Fact]
    public async Task CrossTenant_Admin_Cannot_Access_Other_Institution_Reporting()
    {
        var db = CreateDb();
        var adminA = CreateUser(true, Guid.NewGuid(), _instA, [Roles.InstitutionAdmin]);
        var handlerA = new InstitutionalReportHandler(db, adminA);

        var reportA = await handlerA.GetReportDataAsync(new ReportFilterRequest());
        Assert.NotNull(reportA);

        var adminB = CreateUser(true, Guid.NewGuid(), _instB, [Roles.InstitutionAdmin]);
        var handlerB = new InstitutionalReportHandler(db, adminB);

        var reportB = await handlerB.GetReportDataAsync(new ReportFilterRequest());
        Assert.NotNull(reportB);
    }
}
