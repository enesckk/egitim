using System;
using System.Threading.Tasks;
using EgitimPlatform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Integration;

public class MigrationTests
{
    [Fact]
    public void ModelSnapshot_BuildTargetModel_ExecutesSuccessfully()
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseSqlServer("Server=localhost;Database=DummyTest;Trusted_Connection=True;TrustServerCertificate=True;");
        using var context = new ApplicationDbContext(builder.Options);

        var model = context.Model;
        Assert.NotNull(model);

        // Verify entities registered in EF Core model
        var convEntity = model.FindEntityType("EgitimPlatform.Modules.Communication.Entities.Conversation");
        Assert.NotNull(convEntity);

        var msgEntity = model.FindEntityType("EgitimPlatform.Modules.Communication.Entities.Message");
        Assert.NotNull(msgEntity);

        var meetingEntity = model.FindEntityType("EgitimPlatform.Modules.Coaching.Entities.CoachMeeting");
        Assert.NotNull(meetingEntity);
    }
}
