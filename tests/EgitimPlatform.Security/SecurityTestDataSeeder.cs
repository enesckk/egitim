using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Institutions.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Security;

public static class SecurityTestDataSeeder
{
    // Expose IDs for tests
    public static Guid InstitutionAId { get; private set; }
    public static Guid InstitutionBId { get; private set; }
    public static Guid CoachAUserId { get; private set; }
    public static Guid CoachBUserId { get; private set; }
    public static Guid CoachAEntityId { get; private set; }
    public static Guid AdminAUserId { get; private set; }

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Institutions
        var instA = new Institution { Name = "InstA" };
        var instB = new Institution { Name = "InstB" };
        context.Set<Institution>().AddRange(instA, instB);
        await context.SaveChangesAsync();
        InstitutionAId = instA.Id;
        InstitutionBId = instB.Id;

        // Admin A (InstitutionAdmin at Institution A)
        var adminA = new ApplicationUser
        {
            UserName = "sec-admin-a@test.local",
            Email = "sec-admin-a@test.local",
            FirstName = "AdminA", LastName = "Sec",
            InstitutionId = instA.Id,
            IsActive = true, EmailConfirmed = true,
        };
        await userManager.CreateAsync(adminA, "Test@12345");
        await userManager.AddToRoleAsync(adminA, Roles.InstitutionAdmin);
        AdminAUserId = adminA.Id;

        // Admin B (InstitutionAdmin at Institution B)
        var adminB = new ApplicationUser
        {
            UserName = "sec-admin-b@test.local",
            Email = "sec-admin-b@test.local",
            FirstName = "AdminB", LastName = "Sec",
            InstitutionId = instB.Id,
            IsActive = true, EmailConfirmed = true,
        };
        await userManager.CreateAsync(adminB, "Test@12345");
        await userManager.AddToRoleAsync(adminB, Roles.InstitutionAdmin);

        // Coach A (at Institution A)
        var coachAUser = new ApplicationUser
        {
            UserName = "sec-coach-a@test.local",
            Email = "sec-coach-a@test.local",
            FirstName = "CoachA", LastName = "Sec",
            InstitutionId = instA.Id,
            IsActive = true, EmailConfirmed = true,
        };
        await userManager.CreateAsync(coachAUser, "Test@12345");
        await userManager.AddToRoleAsync(coachAUser, Roles.Coach);
        CoachAUserId = coachAUser.Id;

        // Coach B (at Institution B)
        var coachBUser = new ApplicationUser
        {
            UserName = "sec-coach-b@test.local",
            Email = "sec-coach-b@test.local",
            FirstName = "CoachB", LastName = "Sec",
            InstitutionId = instB.Id,
            IsActive = true, EmailConfirmed = true,
        };
        await userManager.CreateAsync(coachBUser, "Test@12345");
        await userManager.AddToRoleAsync(coachBUser, Roles.Coach);
        CoachBUserId = coachBUser.Id;

        // Coach entities
        var coachEntityA = new Coach { UserId = coachAUser.Id, InstitutionId = instA.Id, FirstName = "CoachA", LastName = "Sec" };
        var coachEntityB = new Coach { UserId = coachBUser.Id, InstitutionId = instB.Id, FirstName = "CoachB", LastName = "Sec" };
        context.Set<Coach>().AddRange(coachEntityA, coachEntityB);
        await context.SaveChangesAsync();
        CoachAEntityId = coachEntityA.Id;
    }
}
