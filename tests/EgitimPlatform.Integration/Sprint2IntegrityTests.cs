using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Academic.Seeding;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace EgitimPlatform.Integration;
[Collection("Integration")]
public class Sprint2IntegrityTests(IntegrationTestFactory factory)
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(factory.ConnectionString).Options);
    private static string Code() => "TEST-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
    [Fact]
    public async Task SeedIsRepeatableAndCreatesTheRealHierarchy()
    {
        using var db = Db(); var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        var levels = new[] { "ExamType", "Subject", "Topic", "SubTopic", "LearningOutcome", "Concept" };
        var records = levels.Select((x, i) => new TaxonomySeedRecord(x, ids[i], Code(), "Synthetic test fixture", i == 0 ? null : ids[i-1])).ToArray();
        var seed = new TaxonomySeeder(db); await seed.SeedAsync(records); await seed.SeedAsync(records);
        Assert.Equal(1, await db.Set<Concept>().CountAsync(x => x.Id == ids[5]));
        Assert.Equal(ids[4], (await db.Set<Concept>().SingleAsync(x => x.Id == ids[5])).LearningOutcomeId);
    }
    [Fact]
    public async Task DuplicateCodeRejectedBySqlIncludingSoftDeletedRecords()
    {
        using var db = Db(); var code = Code(); db.Add(new ExamType { Code = code, Name = "Test", IsDeleted = true }); await db.SaveChangesAsync();
        db.Add(new ExamType { Code = code, Name = "Duplicate" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Theory]
    [InlineData("Subject")] [InlineData("Topic")] [InlineData("SubTopic")] [InlineData("LearningOutcome")] [InlineData("Concept")]
    public async Task MissingHierarchyParentRejectedBySql(string level)
    {
        using var db = Db(); var id = Guid.NewGuid(); var code = Code();
        object row = level switch {
            "Subject" => new Subject { ExamTypeId=id, Code=code, Name="Test" },
            "Topic" => new Topic { SubjectId=id, Code=code, Name="Test" },
            "SubTopic" => new SubTopic { TopicId=id, Code=code, Name="Test" },
            "LearningOutcome" => new LearningOutcome { SubTopicId=id, Code=code, Name="Test" },
            _ => new Concept { LearningOutcomeId=id, Code=code, Name="Test" }
        };
        db.Add(row); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact]
    public async Task GoalRequiresRealExamType()
    {
        using var db = Db(); var inst = new Institution { Name="Test" }; db.Add(inst);
        var student = new Student { InstitutionId=inst.Id, FirstName="Test", LastName="Test" }; db.Add(student); await db.SaveChangesAsync();
        db.Add(new StudentGoal { InstitutionId=inst.Id, StudentId=student.Id, Title="Test", TargetExamTypeId=Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task CrossTenantStudentParentForeignKeysRejected(bool foreignParent)
    {
        using var db = Db(); var a = new Institution { Name="A" }; var b = new Institution { Name="B" }; db.AddRange(a,b);
        var student = new Student { InstitutionId=a.Id, FirstName="T", LastName="T" }; var parent = new Parent { InstitutionId=b.Id, FirstName="P", LastName="P" }; db.AddRange(student,parent); await db.SaveChangesAsync();
        db.Add(new StudentParent { InstitutionId=foreignParent ? a.Id : b.Id, StudentId=student.Id, ParentId=parent.Id, RelationshipType="Guardian" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact]
    public async Task ActiveRelationshipUniqueButInactiveHistoryAllowed()
    {
        using var db = Db(); var i = new Institution { Name="I" }; db.Add(i); var s = new Student { InstitutionId=i.Id, FirstName="S", LastName="T" }; var p = new Parent { InstitutionId=i.Id, FirstName="P", LastName="T" }; db.AddRange(s,p); await db.SaveChangesAsync();
        db.Add(new StudentParent { InstitutionId=i.Id, StudentId=s.Id, ParentId=p.Id, RelationshipType="Guardian", IsActive=false });
        db.Add(new StudentParent { InstitutionId=i.Id, StudentId=s.Id, ParentId=p.Id, RelationshipType="Guardian" }); await db.SaveChangesAsync();
        db.Add(new StudentParent { InstitutionId=i.Id, StudentId=s.Id, ParentId=p.Id, RelationshipType="Guardian" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact]
    public async Task TeacherSubjectRequiresRealSubject()
    {
        using var db = Db(); var inst = new Institution { Name="I" }; db.Add(inst);
        var user = new ApplicationUser { UserName=Code(), FirstName="T", LastName="T", InstitutionId=inst.Id }; db.Add(user);
        var teacher = new Teacher { InstitutionId=inst.Id, UserId=user.Id, FirstName="T", LastName="T" }; db.Add(teacher); await db.SaveChangesAsync();
        db.Add(new TeacherSubject { InstitutionId=inst.Id, TeacherId=teacher.Id, SubjectId=Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact]
    public async Task GoalRowVersionRejectsLostUpdate()
    {
        using var db = Db(); var inst = new Institution { Name="I" }; db.Add(inst); var s = new Student { InstitutionId=inst.Id, FirstName="S", LastName="T" }; db.Add(s);
        var goal = new StudentGoal { InstitutionId=inst.Id, StudentId=s.Id, Title="Before" }; db.Add(goal); await db.SaveChangesAsync();
        using var other = Db(); var stale = await other.Set<StudentGoal>().SingleAsync(x => x.Id == goal.Id);
        goal.Title="First"; await db.SaveChangesAsync(); stale.Title="Second";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => other.SaveChangesAsync());
    }
    [Fact]
    public async Task HistoryIsAppendOnlyAfterPersistence()
    {
        using var db = Db(); var inst = new Institution { Name="I" }; db.Add(inst); var s = new Student { InstitutionId=inst.Id, FirstName="S", LastName="T" }; db.Add(s);
        var goal = new StudentGoal { InstitutionId=inst.Id, StudentId=s.Id, Title="Before" }; db.Add(goal);
        var h = EgitimPlatform.Modules.Students.Services.GoalHistory.Capture(goal,"Created",Guid.NewGuid()); db.Add(h); await db.SaveChangesAsync();
        h.Action="Rewritten"; await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
