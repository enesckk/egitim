using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EgitimPlatform.Security.Fixtures;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudentGoal;
using EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;
using EgitimPlatform.Modules.Students.Features.ManageParents;
using EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace EgitimPlatform.Security;
[Collection("Security")]
public class Sprint2AuthorizationTests(SecurityTestFactory factory)
{
    private async Task<HttpClient> Login(string name)
    {
        var c = factory.CreateClient(); var r = await c.PostAsJsonAsync("/api/v1/auth/login",new LoginCommand($"s2-{name}@test.local","Test@12345"));
        Assert.Equal(HttpStatusCode.OK,r.StatusCode); var dto = await r.Content.ReadFromJsonAsync<LoginResponseDto>(); c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",dto!.AccessToken); return c;
    }
    [Theory]
    [InlineData("student",true)] [InlineData("other",false)] [InlineData("foreign",false)]
    [InlineData("coach",true)] [InlineData("unassigned-coach",false)] [InlineData("foreign-coach",false)]
    [InlineData("admin",true)] [InlineData("foreign-admin",false)]
    [InlineData("parent",true)] [InlineData("unrelated-parent",false)] [InlineData("foreign-parent",false)]
    [InlineData("teacher",false)] [InlineData("foreign-teacher",false)] [InlineData("super",true)]
    public async Task Student360EnforcesRealResourceMatrix(string user,bool allowed)
    {
        using var c=await Login(user); var r=await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/360");
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,r.StatusCode);
        if (allowed) { using var json=JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Assert.Equal(Sprint2TestData.StudentA,json.RootElement.GetProperty("student").GetProperty("id").GetGuid()); }
    }
    [Theory]
    [InlineData("student",true)] [InlineData("other",false)] [InlineData("foreign",false)]
    [InlineData("coach",true)] [InlineData("unassigned-coach",false)] [InlineData("foreign-coach",false)]
    [InlineData("admin",true)] [InlineData("foreign-admin",false)] [InlineData("super",true)]
    [InlineData("parent",false)] [InlineData("teacher",false)]
    public async Task GoalsEnforceOwnershipAndExcludeParentPrivateData(string user,bool allowed)
    {
        using var c=await Login(user); var r=await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/goals");
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,r.StatusCode);
    }
    [Theory]
    [InlineData("student",true)] [InlineData("other",false)] [InlineData("foreign",false)]
    [InlineData("coach",true)] [InlineData("unassigned-coach",false)] [InlineData("foreign-coach",false)]
    [InlineData("admin",true)] [InlineData("foreign-admin",false)] [InlineData("super",true)] [InlineData("parent",false)] [InlineData("teacher",false)]
    public async Task AcademicProfileWriteUsesOwnResourcePolicy(string user,bool allowed)
    {
        using var c=await Login(user); var r=await c.PutAsJsonAsync($"/api/v1/students/{Sprint2TestData.StudentA}/academic-profile",new UpdateAcademicProfileCommand(Sprint2TestData.StudentA,"Test school",null,11,null));
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,r.StatusCode);
    }
    [Fact]
    public async Task StudentCanCreateOwnGoalButNotAnotherStudents()
    {
        using var c=await Login("student");
        var cmd=new CreateStudentGoalCommand(Sprint2TestData.StudentA,"New goal",null,Sprint2TestData.Exam,100,1,null,null);
        var r=await c.PostAsJsonAsync($"/api/v1/students/{cmd.StudentId}/goals",cmd); Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        cmd=cmd with { StudentId=Sprint2TestData.StudentOther }; r=await c.PostAsJsonAsync($"/api/v1/students/{cmd.StudentId}/goals",cmd); Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);
    }
    [Fact]
    public async Task InactiveGoalCannotBypassResourceAuthorization()
    {
        using var c=await Login("student"); var r=await c.DeleteAsync($"/api/v1/students/goals/{Sprint2TestData.InactiveGoalOther}"); Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);
    }
    [Fact]
    public async Task ParentGetsSafeCompositionOnly()
    {
        using var c=await Login("parent"); var r=await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/360"); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var body=await r.Content.ReadAsStringAsync(); Assert.DoesNotContain("PRIVATE-",body); Assert.DoesNotContain("description",body); Assert.DoesNotContain("history",body); Assert.DoesNotContain("userId",body);
        var history=await c.GetAsync($"/api/v1/students/goals/{Sprint2TestData.GoalA}/history"); Assert.Equal(HttpStatusCode.Forbidden,history.StatusCode);
        var profile=await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}"); Assert.Equal(HttpStatusCode.OK,profile.StatusCode);
    }
    [Fact]
    public async Task ParentCannotReadUnrelatedSameInstitutionChild()
    {
        using var c=await Login("parent"); var r=await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentOther}/360"); Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);
    }
    [Fact]
    public async Task InactiveAndDeletedRelationsDoNotGrantAccess()
    {
        // Dedicated relationship keeps the shared matrix fixture immutable.
        using var admin=await Login("admin");
        using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(factory.ConnectionString).Options);
        var child=new Student { InstitutionId=Sprint2TestData.InstitutionA,FirstName="Transient",LastName="Child" }; db.Add(child); await db.SaveChangesAsync();
        var r=await admin.PostAsJsonAsync("/api/v1/parents/relationships",new LinkParentCommand(child.Id,Sprint2TestData.ParentA,"Guardian")); Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        var link=(await r.Content.ReadFromJsonAsync<StudentParentDto>())!;
        using var parent=await Login("parent"); Assert.Equal(HttpStatusCode.OK,(await parent.GetAsync($"/api/v1/students/{child.Id}/360")).StatusCode);
        r=await admin.PutAsJsonAsync($"/api/v1/parents/relationships/{link.Id}",new SetParentRelationshipCommand(link.Id,false)); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await parent.GetAsync($"/api/v1/students/{child.Id}/360")).StatusCode);
        r=await admin.PutAsJsonAsync($"/api/v1/parents/relationships/{link.Id}",new SetParentRelationshipCommand(link.Id,true)); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var entity=await db.Set<StudentParent>().SingleAsync(x=>x.Id==link.Id); entity.IsDeleted=true; entity.DeletedAt=DateTimeOffset.UtcNow; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden,(await parent.GetAsync($"/api/v1/students/{child.Id}/360")).StatusCode);
    }
    [Fact]
    public async Task InvalidExamAndSubjectReferencesAreRejectedByApi()
    {
        using var c=await Login("admin"); var cmd=new CreateStudentGoalCommand(Sprint2TestData.StudentA,"Invalid exam",null,Guid.NewGuid(),null,null,null,null);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync($"/api/v1/students/{cmd.StudentId}/goals",cmd)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync($"/api/v1/teachers/{Sprint2TestData.TeacherA}/subjects/{Guid.NewGuid()}",null)).StatusCode);
    }
    [Fact]
    public async Task AdminCannotCreateCrossInstitutionParentRelationship()
    {
        using var c=await Login("admin"); Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/v1/parents/relationships",new LinkParentCommand(Sprint2TestData.StudentA,Sprint2TestData.ParentForeign,"Guardian"))).StatusCode);
    }
    [Fact]
    public async Task TeacherSubjectAssignmentDoesNotGrantStudentAccess()
    {
        using var c=await Login("teacher"); Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentForeign}/360")).StatusCode);
    }
    [Theory]
    [InlineData("teacher",true)] [InlineData("admin",true)] [InlineData("foreign-teacher",false)] [InlineData("foreign-admin",false)] [InlineData("student",false)]
    public async Task TeacherSubjectScopeIsOwnOrInstitutionManaged(string user, bool allowed)
    {
        using var c=await Login(user); var r=await c.GetAsync($"/api/v1/teachers/{Sprint2TestData.TeacherA}/subjects");
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,r.StatusCode);
        if (allowed) Assert.Contains(Sprint2TestData.Subject.ToString(), await r.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task SuperAdminCanViewForeignInstitutionExplicitly()
    {
        using var c=await Login("super"); Assert.Equal(HttpStatusCode.OK,(await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentForeign}/360")).StatusCode);
    }
    [Fact]
    public async Task FoundationCreationValidatesAccountTenantAndAuditsChanges()
    {
        using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(factory.ConnectionString).Options);
        var newParent = await db.Set<EgitimPlatform.Modules.Identity.Entities.ApplicationUser>().SingleAsync(x=>x.Email=="s2-new-parent@test.local");
        var newTeacher = await db.Set<EgitimPlatform.Modules.Identity.Entities.ApplicationUser>().SingleAsync(x=>x.Email=="s2-new-teacher@test.local");
        var foreignParent = await db.Set<EgitimPlatform.Modules.Identity.Entities.ApplicationUser>().SingleAsync(x=>x.Email=="s2-foreign-parent@test.local");
        using var c=await Login("admin");
        var bad=await c.PostAsJsonAsync("/api/v1/parents",new CreateParentCommand(Sprint2TestData.InstitutionA,foreignParent.Id,"Foreign","Parent"));
        Assert.Equal(HttpStatusCode.BadRequest,bad.StatusCode);
        var p=await c.PostAsJsonAsync("/api/v1/parents",new CreateParentCommand(Sprint2TestData.InstitutionA,newParent.Id,"New","Parent")); Assert.Equal(HttpStatusCode.Created,p.StatusCode);
        var parent=(await p.Content.ReadFromJsonAsync<ParentDto>())!;
        var t=await c.PostAsJsonAsync("/api/v1/teachers",new CreateTeacherCommand(Sprint2TestData.InstitutionA,newTeacher.Id,"New","Teacher")); Assert.Equal(HttpStatusCode.Created,t.StatusCode);
        var teacher=(await t.Content.ReadFromJsonAsync<TeacherDto>())!;
        var subject=await c.PostAsync($"/api/v1/teachers/{teacher.Id}/subjects/{Sprint2TestData.Subject}",null); Assert.Equal(HttpStatusCode.Created,subject.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsync($"/api/v1/teachers/{teacher.Id}/subjects/{Sprint2TestData.Subject}",null)).StatusCode);
        Assert.True(await db.Set<EgitimPlatform.Modules.Identity.Entities.AuditLog>().AnyAsync(x=>x.EntityId==parent.Id.ToString() && x.Action=="Parent.Created"));
        Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync($"/api/v1/teachers/{teacher.Id}/subjects/{Sprint2TestData.Subject}")).StatusCode);
    }
    [Fact]
    public async Task AnonymousCannotAccessStudent360OrTaxonomy()
    {
        using var c=factory.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/360")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/academic/exam-types")).StatusCode);
    }
    [Fact]
    public async Task AuthenticatedTaxonomyUsesRealIdsAndRejectsInvalidQuery()
    {
        using var c=await Login("student"); var r=await c.GetAsync($"/api/v1/academic/subjects?parentId={Sprint2TestData.Exam}"); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        Assert.Contains(Sprint2TestData.Subject.ToString(),await r.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/v1/academic/subjects?pageSize=10000")).StatusCode);
    }
}
