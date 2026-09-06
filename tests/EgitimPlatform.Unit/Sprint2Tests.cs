using System.Diagnostics;
using System.Text.Json;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;
using EgitimPlatform.Modules.Students.Features.UpdateStudentGoal;
using EgitimPlatform.Modules.Students.Features.GetStudent360;
using EgitimPlatform.Modules.Students.Features.ManageParents;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Academic.Features;
using EgitimPlatform.Modules.Academic.Seeding;
using EgitimPlatform.Modules.Teachers.Entities;
using EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
namespace EgitimPlatform.Unit;
public class Sprint2Tests
{
    private static ApplicationDbContext ModelContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=true;TrustServerCertificate=true").Options);
    [Theory]
    [InlineData("Student", true)] [InlineData("Coach", true)] [InlineData("InstitutionAdmin", true)]
    [InlineData("SuperAdmin", true)] [InlineData("Teacher", false)] [InlineData("Parent", false)] [InlineData("Unknown", false)]
    public async Task ResourceAuthorizationRequiresSupportedRoleAndOwnership(string role, bool allowed)
    {
        var user = Substitute.For<ICurrentUser>(); var coaches = Substitute.For<ICoachStudentQuery>();
        var institution = Guid.NewGuid(); var userId = Guid.NewGuid();
        user.IsAuthenticated.Returns(true); user.UserId.Returns(userId); user.GetInstitutionIdAsync().Returns((Guid?)institution);
        user.IsInRole(role).Returns(true); user.IsSuperAdmin.Returns(role == Roles.SuperAdmin);
        var student = new Student { UserId = userId, InstitutionId = institution };
        coaches.HasActiveAssignmentAsync(userId, institution, student.Id, default).Returns(true);
        var action = () => GoalAuthorizationHelper.AuthorizeForStudentAsync(student, user, coaches, default);
        if (allowed) await action(); else await Assert.ThrowsAsync<ForbiddenException>(action);
    }
    [Theory]
    [InlineData("Student")] [InlineData("Coach")] [InlineData("InstitutionAdmin")]
    public async Task CrossInstitutionIsRejectedEvenWithMatchingUserOrAssignment(string role)
    {
        var user = Substitute.For<ICurrentUser>(); var coaches = Substitute.For<ICoachStudentQuery>();
        user.IsAuthenticated.Returns(true); user.UserId.Returns(Guid.NewGuid()); user.IsInRole(role).Returns(true);
        user.GetInstitutionIdAsync().Returns((Guid?)Guid.NewGuid());
        var student = new Student { UserId = user.UserId, InstitutionId = Guid.NewGuid() };
        await Assert.ThrowsAsync<ForbiddenException>(() => GoalAuthorizationHelper.AuthorizeForStudentAsync(student, user, coaches, default));
    }
    [Fact]
    public async Task UnassignedCoachAndOtherStudentAreRejected()
    {
        var user = Substitute.For<ICurrentUser>(); var coaches = Substitute.For<ICoachStudentQuery>();
        var inst = Guid.NewGuid(); user.GetInstitutionIdAsync().Returns((Guid?)inst); user.UserId.Returns(Guid.NewGuid()); user.IsAuthenticated.Returns(true);
        var student = new Student { InstitutionId = inst, UserId = Guid.NewGuid() };
        user.IsInRole(Roles.Coach).Returns(true);
        await Assert.ThrowsAsync<ForbiddenException>(() => GoalAuthorizationHelper.AuthorizeForStudentAsync(student, user, coaches, default));
        user.IsInRole(Roles.Coach).Returns(false); user.IsInRole(Roles.Student).Returns(true);
        await Assert.ThrowsAsync<ForbiddenException>(() => GoalAuthorizationHelper.AuthorizeForStudentAsync(student, user, coaches, default));
    }
    [Fact]
    public void HistoryCapturesExamStatusActorAndTrace()
    {
        using var trace = new Activity("goal-test").Start();
        var actor = Guid.NewGuid(); var goal = new StudentGoal { TargetExamTypeId = Guid.NewGuid(), Title = "Test", IsActive = false };
        var entry = GoalHistory.Capture(goal, "Deactivated", actor, "{}");
        using var json = JsonDocument.Parse(entry.NewValuesJson!);
        Assert.Equal(goal.TargetExamTypeId, json.RootElement.GetProperty("TargetExamTypeId").GetGuid());
        Assert.False(json.RootElement.GetProperty("IsActive").GetBoolean());
        Assert.Equal(actor, entry.ChangedBy); Assert.Equal(trace.TraceId.ToString(), entry.CorrelationId);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task HistoryCannotBeUpdatedOrDeleted(bool delete)
    {
        using var db = ModelContext(); var entry = new StudentGoalHistory { Action = "Created" }; db.Attach(entry);
        if (delete) db.Remove(entry); else entry.Action = "Altered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
    }
    [Fact]
    public void ModelContainsAllModulesAndRelationalConstraints()
    {
        using var db = ModelContext();
        foreach (var type in new[] { typeof(Teacher), typeof(TeacherSubject), typeof(StudentParent), typeof(ExamType), typeof(Subject), typeof(Topic), typeof(SubTopic), typeof(LearningOutcome), typeof(Concept) })
            Assert.NotNull(db.Model.FindEntityType(type));
        var links = db.Model.FindEntityType(typeof(StudentParent))!;
        Assert.Equal(2, links.GetForeignKeys().Count(x => x.Properties.Count == 2));
        Assert.Contains(db.Model.FindEntityType(typeof(StudentGoal))!.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(ExamType));
        Assert.Contains(db.Model.FindEntityType(typeof(TeacherSubject))!.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(Subject));
    }
    [Fact]
    public void TaxonomyAncestorQueryTranslatesToSql()
    {
        using var db = ModelContext(); var handler = new TaxonomyHandler(db);
        var property = typeof(TaxonomyHandler).GetProperty("Concepts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var query = (IQueryable)property.GetValue(handler)!;
        Assert.Contains("AcademicExamTypes", query.ToQueryString());
    }
    [Fact]
    public async Task StableTaxonomyCodeCannotBeRewritten()
    {
        using var db = ModelContext(); var exam = new ExamType { Code = "TEST-OLD", Name = "Test" }; db.Attach(exam); exam.Code = "TEST-NEW";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
    [Theory]
    [InlineData(0)] [InlineData(13)]
    public void InvalidGradeRejected(int grade) => Assert.False(new UpdateAcademicProfileValidator().Validate(new UpdateAcademicProfileCommand(Guid.NewGuid(), null, null, grade, null)).IsValid);
    [Fact]
    public void CommandsRejectEmptyIdentitiesAndBlankGoalTitle()
    {
        Assert.False(new GetStudent360Validator().Validate(new GetStudent360Query(Guid.Empty)).IsValid);
        Assert.False(new LinkParentValidator().Validate(new LinkParentCommand(Guid.Empty, Guid.Empty, "Other")).IsValid);
        Assert.False(new CreateParentValidator().Validate(new CreateParentCommand(Guid.Empty, null, "", "")).IsValid);
        Assert.False(new CreateTeacherValidator().Validate(new CreateTeacherCommand(Guid.Empty, Guid.Empty, "", "")).IsValid);
        Assert.False(new TeacherSubjectValidator().Validate(new TeacherSubjectCommand(Guid.Empty, Guid.Empty)).IsValid);
        Assert.False(new UpdateStudentGoalValidator().Validate(new UpdateStudentGoalCommand(Guid.NewGuid(), "", null, null, null, null, null, null)).IsValid);
    }
    [Theory]
    [InlineData("bad code")] [InlineData("")] [InlineData("lowercase")]
    public void SeedRejectsUnstableCodes(string code) => Assert.False(new TaxonomySeedValidator().Validate(new TaxonomySeedRecord("ExamType", Guid.NewGuid(), code, "Test", null)).IsValid);
    [Fact]
    public void TaxonomyQueryRejectsUnknownLevelAndUnboundedPage() => Assert.False(new TaxonomyQueryValidator().Validate(new TaxonomyQuery("invented", null, 0, 10000)).IsValid);
}
