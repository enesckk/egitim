using System;
using System.Threading.Tasks;
using EgitimPlatform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Integration;

public class FreshDatabaseMigrationIntegrationTests
{
    [Fact]
    public async Task All_EF_Migrations_Apply_To_Fresh_MSSQL_Database_Successfully()
    {
        var connStr = "Server=localhost,14333;Database=FreshMigrationTestDb;User Id=sa;Password=TestPassword123!;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connStr)
            .Options;

        using var db = new ApplicationDbContext(options);

        // Apply all migrations on fresh database
        await db.Database.MigrateAsync();

        // Verify key tables exist and can be queried
        var canQueryConvs = await db.Database.CanConnectAsync();
        Assert.True(canQueryConvs);

        var convTableExists = await db.Database.ExecuteSqlRawAsync("SELECT TOP 1 1 FROM [Conversations]") >= -1;
        Assert.True(convTableExists);

        var msgTableExists = await db.Database.ExecuteSqlRawAsync("SELECT TOP 1 1 FROM [Messages]") >= -1;
        Assert.True(msgTableExists);

        var coachMeetingsTableExists = await db.Database.ExecuteSqlRawAsync("SELECT TOP 1 1 FROM [CoachMeetings]") >= -1;
        Assert.True(coachMeetingsTableExists);
    }
}
