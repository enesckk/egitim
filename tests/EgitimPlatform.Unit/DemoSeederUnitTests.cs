using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Api.Seeding;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class DemoSeederUnitTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static (UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager) CreateIdentityManagers(ApplicationDbContext db)
    {
        var userStore = NSubstitute.Substitute.For<IUserStore<ApplicationUser>>();
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = NSubstitute.Substitute.For<IRoleStore<ApplicationRole>>();
        var roleManager = new RoleManager<ApplicationRole>(
            roleStore,
            null!, null!, null!, null!);

        return (userManager, roleManager);
    }

    [Fact]
    public async Task SeedAsync_WhenDisabled_DoesNotSeedData()
    {
        using var db = CreateDbContext();
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);

        var settings = Options.Create(new DemoSeedSettings { Enabled = false });

        var (userManager, roleManager) = CreateIdentityManagers(db);
        var seeder = new DemoDataSeeder(db, userManager, roleManager, settings, env, NullLogger<DemoDataSeeder>.Instance);

        await seeder.SeedAsync();

        var inst = await db.Set<Institution>().FirstOrDefaultAsync(i => i.Id == DemoDataSeeder.DemoInstitutionId);
        Assert.Null(inst);
    }

    [Fact]
    public async Task SeedAsync_WhenEnabledWithoutPasswords_ThrowsInvalidOperationException()
    {
        using var db = CreateDbContext();
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);

        var settings = Options.Create(new DemoSeedSettings { Enabled = true, AdminPassword = null });

        var (userManager, roleManager) = CreateIdentityManagers(db);
        var seeder = new DemoDataSeeder(db, userManager, roleManager, settings, env, NullLogger<DemoDataSeeder>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
    }

    [Fact]
    public async Task SeedAsync_WhenEnabledWithValidConfig_SeedsShowcaseDatasetIdempotently()
    {
        using var db = CreateDbContext();
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);

        var settings = Options.Create(new DemoSeedSettings
        {
            Enabled = true,
            AdminPassword = "DemoPassword123!",
            TeacherPassword = "DemoPassword123!",
            CoachPassword = "DemoPassword123!",
            StudentPassword = "DemoPassword123!",
            ParentPassword = "DemoPassword123!"
        });

        // Mock UserManager and RoleManager for memory DB
        var userStore = Substitute.For<IUserStore<ApplicationUser>, IUserEmailStore<ApplicationUser>>();
        var userManager = Substitute.For<UserManager<ApplicationUser>>(userStore, null, null, null, null, null, null, null, null);
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);
        userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);

        var roleStore = Substitute.For<IRoleStore<ApplicationRole>>();
        var roleManager = Substitute.For<RoleManager<ApplicationRole>>(roleStore, null, null, null, null);
        roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(true);

        var seeder = new DemoDataSeeder(db, userManager, roleManager, settings, env, NullLogger<DemoDataSeeder>.Instance);

        // First Run
        await seeder.SeedAsync();

        // Verify Institution
        var inst = await db.Set<Institution>().IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == DemoDataSeeder.DemoInstitutionId);
        Assert.NotNull(inst);
        Assert.Equal("Demo Eğitim Kurumu", inst.Name);

        // Verify Student
        var student = await db.Set<Student>().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == DemoDataSeeder.DemoStudentId);
        Assert.NotNull(student);
        Assert.Equal("Ahmet", student.FirstName);
        Assert.Equal("Yılmaz", student.LastName);
        Assert.Equal(12, student.GradeLevel);

        // Verify StudentParent link
        var sp = await db.Set<StudentParent>().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.StudentId == DemoDataSeeder.DemoStudentId);
        Assert.NotNull(sp);

        // Verify Coach Assignment
        var ca = await db.Set<StudentCoachAssignment>().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.StudentId == DemoDataSeeder.DemoStudentId);
        Assert.NotNull(ca);

        // Verify 10 Chronological Exams & 300 Attempts (Phase 1 Expansion)
        var exams = await db.Set<Exam>().IgnoreQueryFilters().Where(e => e.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(10, exams.Count);

        var attempts = await db.Set<StudentExamAttempt>().IgnoreQueryFilters().Where(a => a.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(300, attempts.Count);

        var subjectResults = await db.Set<StudentExamSubjectResult>().IgnoreQueryFilters().Where(r => r.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(1200, subjectResults.Count);

        var topicResults = await db.Set<StudentExamTopicResult>().IgnoreQueryFilters().Where(r => r.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(2400, topicResults.Count);

        var students = await db.Set<Student>().IgnoreQueryFilters().Where(s => s.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(30, students.Count);

        var classrooms = await db.Set<Classroom>().IgnoreQueryFilters().Where(c => c.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(3, classrooms.Count);

        var teachers = await db.Set<Teacher>().IgnoreQueryFilters().Where(t => t.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(5, teachers.Count);

        var coaches = await db.Set<Coach>().IgnoreQueryFilters().Where(c => c.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(3, coaches.Count);

        var parents = await db.Set<Parent>().IgnoreQueryFilters().Where(p => p.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.Equal(10, parents.Count);

        // Verify Historical CoachTask
        var coachTask = await db.Set<CoachTask>().IgnoreQueryFilters().FirstOrDefaultAsync(t => t.InstitutionId == DemoDataSeeder.DemoInstitutionId);
        Assert.NotNull(coachTask);
        Assert.Equal("Paragraf Tekrar Çalışması", coachTask.Title);
        Assert.Equal(CoachTaskStatus.Completed, coachTask.Status);

        // Verify Notifications
        var notifications = await db.Set<Notification>().IgnoreQueryFilters().Where(n => n.InstitutionId == DemoDataSeeder.DemoInstitutionId).ToListAsync();
        Assert.True(notifications.Count >= 2);

        // Second Run (Idempotency test)
        await seeder.SeedAsync();

        var instCount = await db.Set<Institution>().IgnoreQueryFilters().CountAsync(i => i.Id == DemoDataSeeder.DemoInstitutionId);
        Assert.Equal(1, instCount);

        var attemptCount = await db.Set<StudentExamAttempt>().IgnoreQueryFilters().CountAsync(a => a.InstitutionId == DemoDataSeeder.DemoInstitutionId);
        Assert.Equal(300, attemptCount);

        var subjectCount = await db.Set<StudentExamSubjectResult>().IgnoreQueryFilters().CountAsync(r => r.InstitutionId == DemoDataSeeder.DemoInstitutionId);
        Assert.Equal(1200, subjectCount);

        var topicCount = await db.Set<StudentExamTopicResult>().IgnoreQueryFilters().CountAsync(r => r.InstitutionId == DemoDataSeeder.DemoInstitutionId);
        Assert.Equal(2400, topicCount);
    }
}
