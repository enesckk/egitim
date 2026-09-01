using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Institutions.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Integration.TestData;

public static class TestDataSeeder
{
    private static bool _seeded;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public static async Task SeedTestDataAsync(IServiceProvider services)
    {
        if (_seeded) return;
        await _lock.WaitAsync();
        try
        {
            if (_seeded) return;

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Only seed if institutions don't exist yet
            if (await context.Set<Institution>().AnyAsync())
            {
                _seeded = true;
                return;
            }

            // Create institutions
            var institutionA = new Institution { Name = "Institution A" };
            var institutionB = new Institution { Name = "Institution B" };
            context.Set<Institution>().AddRange(institutionA, institutionB);
            await context.SaveChangesAsync();

            // Create users
            var institutionAdminA = new ApplicationUser
            {
                UserName = "admin-a@test.local",
                Email = "admin-a@test.local",
                FirstName = "Admin",
                LastName = "A",
                InstitutionId = institutionA.Id,
                IsActive = true,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(institutionAdminA, "Test@12345");
            await userManager.AddToRoleAsync(institutionAdminA, Roles.InstitutionAdmin);

            var coachA = new ApplicationUser
            {
                UserName = "coach-a@test.local",
                Email = "coach-a@test.local",
                FirstName = "Coach",
                LastName = "A",
                InstitutionId = institutionA.Id,
                IsActive = true,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(coachA, "Test@12345");
            await userManager.AddToRoleAsync(coachA, Roles.Coach);

            var institutionAdminB = new ApplicationUser
            {
                UserName = "admin-b@test.local",
                Email = "admin-b@test.local",
                FirstName = "Admin",
                LastName = "B",
                InstitutionId = institutionB.Id,
                IsActive = true,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(institutionAdminB, "Test@12345");
            await userManager.AddToRoleAsync(institutionAdminB, Roles.InstitutionAdmin);

            var coachB = new ApplicationUser
            {
                UserName = "coach-b@test.local",
                Email = "coach-b@test.local",
                FirstName = "Coach",
                LastName = "B",
                InstitutionId = institutionB.Id,
                IsActive = true,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(coachB, "Test@12345");
            await userManager.AddToRoleAsync(coachB, Roles.Coach);

            // Create coach domain entities
            var coachEntityA = new Coach { UserId = coachA.Id, InstitutionId = institutionA.Id, FirstName = "Coach", LastName = "A" };
            var coachEntityB = new Coach { UserId = coachB.Id, InstitutionId = institutionB.Id, FirstName = "Coach", LastName = "B" };
            context.Set<Coach>().AddRange(coachEntityA, coachEntityB);
            await context.SaveChangesAsync();

            _seeded = true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
