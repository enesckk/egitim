using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EgitimPlatform.Security.Fixtures;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudentGoal;
using EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;
using EgitimPlatform.Modules.Students.Features.UpdateStudentGoal;
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
    [InlineData("student",HttpStatusCode.OK)] [InlineData("other",HttpStatusCode.Forbidden)] [InlineData("foreign",HttpStatusCode.NotFound)]
    [InlineData("coach",HttpStatusCode.OK)] [InlineData("unassigned-coach",HttpStatusCode.Forbidden)] [InlineData("foreign-coach",HttpStatusCode.NotFound)]
    [InlineData("admin",HttpStatusCode.OK)] [InlineData("foreign-admin",HttpStatusCode.NotFound)]
    [InlineData("parent",HttpStatusCode.OK)] [InlineData("unrelated-parent",HttpStatusCode.Forbidden)] [InlineData("foreign-parent",HttpStatusCode.NotFound)]
    [InlineData("teacher",HttpStatusCode.Forbidden)] [InlineData("foreign-teacher",HttpStatusCode.NotFound)] [InlineData("super",HttpStatusCode.OK)]
    public async Task Student360EnforcesTenantHidingAndResourceMatrix(string user,HttpStatusCode expected)
    {
        using var c=await Login(user); var r=await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/360");
        Assert.Equal(expected,r.StatusCode);
        if (expected == HttpStatusCode.OK) { using var json=JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Assert.Equal(Sprint2TestData.StudentA,json.RootElement.GetProperty("student").GetProperty("id").GetGuid()); }
    }
    [Theory]
    [InlineData("student",HttpStatusCode.OK)] [InlineData("other",HttpStatusCode.Forbidden)] [InlineData("foreign",HttpStatusCode.NotFound)]
    [InlineData("coach",HttpStatusCode.OK)] [InlineData("unassigned-coach",HttpStatusCode.Forbidden)] [InlineData("foreign-coach",HttpStatusCode.NotFound)]
    [InlineData("admin",HttpStatusCode.OK)] [InlineData("foreign-admin",HttpStatusCode.NotFound)] [InlineData("super",HttpStatusCode.OK)]
    [InlineData("parent",HttpStatusCode.Forbidden)] [InlineData("teacher",HttpStatusCode.Forbidden)]
    public async Task GoalListEnforcesTenantHidingOwnershipAndPrivacy(string user,HttpStatusCode expected)
    {
        using var c=await Login(user); var r=await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}/goals");
        Assert.Equal(expected,r.StatusCode);
    }
    [Fact]
    public async Task Student360ForeignAndMissingStudentsBothReturnNotFound()
    {
        using var c=await Login("admin");
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentForeign}/360")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/students/{Guid.NewGuid()}/360")).StatusCode);
    }
    [Fact]
    public async Task GoalListForeignAndMissingStudentsBothReturnNotFound()
    {
        using var c=await Login("admin");
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentForeign}/goals")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/students/{Guid.NewGuid()}/goals")).StatusCode);
    }
    [Theory]
    [InlineData("student",HttpStatusCode.OK)] [InlineData("other",HttpStatusCode.Forbidden)] [InlineData("foreign",HttpStatusCode.NotFound)]
    [InlineData("coach",HttpStatusCode.OK)] [InlineData("unassigned-coach",HttpStatusCode.Forbidden)] [InlineData("foreign-coach",HttpStatusCode.NotFound)]
    [InlineData("admin",HttpStatusCode.OK)] [InlineData("foreign-admin",HttpStatusCode.NotFound)] [InlineData("super",HttpStatusCode.OK)]
    [InlineData("parent",HttpStatusCode.Forbidden)] [InlineData("teacher",HttpStatusCode.Forbidden)]
    public async Task AcademicProfileWriteUsesTenantHidingAndOwnResourcePolicy(string user,HttpStatusCode expected)
    {
        using var c=await Login(user); var r=await c.PutAsJsonAsync($"/api/v1/students/{Sprint2TestData.StudentA}/academic-profile",new UpdateAcademicProfileCommand(Sprint2TestData.StudentA,"Test school",null,11,null));
        Assert.Equal(expected,r.StatusCode);
    }
    [Fact]
    public async Task AcademicProfileForeignAndMissingStudentsBothReturnNotFound()
    {
        using var c=await Login("admin");
        var foreign=new UpdateAcademicProfileCommand(Sprint2TestData.StudentForeign,"Hidden school",null,10,null);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync($"/api/v1/students/{foreign.StudentId}/academic-profile",foreign)).StatusCode);
        var missing=foreign with { StudentId=Guid.NewGuid() };
        Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync($"/api/v1/students/{missing.StudentId}/academic-profile",missing)).StatusCode);
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
    public async Task CreateGoalForeignAndMissingStudentsBothReturnNotFound()
    {
        using var c=await Login("admin");
        var foreign=new CreateStudentGoalCommand(Sprint2TestData.StudentForeign,"Hidden goal",null,null,null,null,null,null);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync($"/api/v1/students/{foreign.StudentId}/goals",foreign)).StatusCode);
        var missing=foreign with { StudentId=Guid.NewGuid() };
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync($"/api/v1/students/{missing.StudentId}/goals",missing)).StatusCode);
    }
    [Fact]
    public async Task InactiveGoalCannotBypassResourceAuthorization()
    {
        using var c=await Login("student"); var r=await c.DeleteAsync($"/api/v1/students/goals/{Sprint2TestData.InactiveGoalOther}"); Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);
    }
    [Fact]
    public async Task ForeignGoalDetailReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/students/goals/{Sprint2TestData.GoalForeign}");
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task GoalHistoryForeignAndMissingGoalsBothReturnNotFound()
    {
        using var c=await Login("admin");
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/students/goals/{Sprint2TestData.GoalForeign}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/students/goals/{Guid.NewGuid()}/history")).StatusCode);
    }
    [Fact]
    public async Task GoalHistorySameTenantUnauthorizedIsForbiddenAndSuperAdminCanReadForeign()
    {
        using var other=await Login("other");
        Assert.Equal(HttpStatusCode.Forbidden,(await other.GetAsync($"/api/v1/students/goals/{Sprint2TestData.GoalA}/history")).StatusCode);
        using var super=await Login("super");
        Assert.Equal(HttpStatusCode.OK,(await super.GetAsync($"/api/v1/students/goals/{Sprint2TestData.GoalForeign}/history")).StatusCode);
    }
    [Fact]
    public async Task UpdateGoalForeignAndMissingGoalsBothReturnNotFound()
    {
        using var c=await Login("admin");
        var foreign=new UpdateStudentGoalCommand(Sprint2TestData.GoalForeign,"Hidden update",null,null,null,null,null,null);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync($"/api/v1/students/goals/{foreign.GoalId}",foreign)).StatusCode);
        var missing=foreign with { GoalId=Guid.NewGuid() };
        Assert.Equal(HttpStatusCode.NotFound,(await c.PutAsJsonAsync($"/api/v1/students/goals/{missing.GoalId}",missing)).StatusCode);
    }
    [Fact]
    public async Task UpdateGoalSameTenantUnauthorizedReturnsForbidden()
    {
        using var c=await Login("other");
        var command=new UpdateStudentGoalCommand(Sprint2TestData.GoalA,"Denied update",null,null,null,null,null,null);
        Assert.Equal(HttpStatusCode.Forbidden,(await c.PutAsJsonAsync($"/api/v1/students/goals/{command.GoalId}",command)).StatusCode);
    }
    [Fact]
    public async Task DeactivateGoalForeignAndMissingGoalsBothReturnNotFound()
    {
        using var c=await Login("admin");
        Assert.Equal(HttpStatusCode.NotFound,(await c.DeleteAsync($"/api/v1/students/goals/{Sprint2TestData.GoalForeign}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.DeleteAsync($"/api/v1/students/goals/{Guid.NewGuid()}")).StatusCode);
    }
    [Fact]
    public async Task SuperAdminCanManageForeignStudentGoalAndAcademicProfile()
    {
        using var c=await Login("super");
        var createCommand=new CreateStudentGoalCommand(Sprint2TestData.StudentForeign,"SuperAdmin foreign goal",null,null,null,null,null,null);
        var create=await c.PostAsJsonAsync($"/api/v1/students/{createCommand.StudentId}/goals",createCommand);
        Assert.Equal(HttpStatusCode.Created,create.StatusCode);
        var goal=(await create.Content.ReadFromJsonAsync<StudentGoalDto>())!;

        var updateCommand=new UpdateStudentGoalCommand(goal.Id,"SuperAdmin updated goal",null,null,null,null,null,null);
        Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync($"/api/v1/students/goals/{goal.Id}",updateCommand)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync($"/api/v1/students/goals/{goal.Id}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync($"/api/v1/students/goals/{goal.Id}")).StatusCode);

        var profileCommand=new UpdateAcademicProfileCommand(Sprint2TestData.StudentForeign,"SuperAdmin school",null,10,null);
        Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync($"/api/v1/students/{profileCommand.StudentId}/academic-profile",profileCommand)).StatusCode);
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
        // Tenant-scoped lookup: cross-tenant parent returns 404 (no information leak about existence)
        using var c=await Login("admin"); Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync("/api/v1/parents/relationships",new LinkParentCommand(Sprint2TestData.StudentA,Sprint2TestData.ParentForeign,"Guardian"))).StatusCode);
    }
    [Fact]
    public async Task TeacherSubjectAssignmentDoesNotGrantStudentAccess()
    {
        using var c=await Login("teacher"); Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v1/students/{Sprint2TestData.StudentForeign}/360")).StatusCode);
    }
    [Theory]
    [InlineData("teacher",HttpStatusCode.OK)] [InlineData("admin",HttpStatusCode.OK)]
    [InlineData("foreign-teacher",HttpStatusCode.NotFound)] [InlineData("foreign-admin",HttpStatusCode.NotFound)]
    [InlineData("student",HttpStatusCode.Forbidden)]
    public async Task TeacherSubjectScopeIsOwnOrInstitutionManaged(string user, HttpStatusCode expected)
    {
        using var c=await Login(user); var r=await c.GetAsync($"/api/v1/teachers/{Sprint2TestData.TeacherA}/subjects");
        Assert.Equal(expected,r.StatusCode);
        if (expected == HttpStatusCode.OK) Assert.Contains(Sprint2TestData.Subject.ToString(), await r.Content.ReadAsStringAsync());
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
    // ── Teacher List/Detail Endpoint Tests ──
    [Fact]
    public async Task TeacherListReturnsSameInstitutionOnly()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync("/api/v1/teachers"); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var body=await r.Content.ReadAsStringAsync();
        Assert.Contains(Sprint2TestData.TeacherA.ToString(),body);
        Assert.DoesNotContain(Sprint2TestData.TeacherB.ToString(),body);
    }
    [Fact]
    public async Task TeacherListExcludesCrossInstitution()
    {
        using var c=await Login("foreign-admin");
        var r=await c.GetAsync("/api/v1/teachers"); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var body=await r.Content.ReadAsStringAsync();
        Assert.Contains(Sprint2TestData.TeacherB.ToString(),body);
        Assert.DoesNotContain(Sprint2TestData.TeacherA.ToString(),body);
    }
    [Theory]
    [InlineData("student",false)] [InlineData("parent",false)] [InlineData("teacher",false)] [InlineData("coach",false)]
    [InlineData("admin",true)] [InlineData("super",true)]
    public async Task TeacherListEnforcesAuthorization(string user, bool allowed)
    {
        using var c=await Login(user); var r=await c.GetAsync("/api/v1/teachers");
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, r.StatusCode);
    }
    [Fact]
    public async Task TeacherDetailCrossInstitutionReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/teachers/{Sprint2TestData.TeacherB}");
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task TeacherDetailSameInstitutionSucceeds()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/teachers/{Sprint2TestData.TeacherA}");
        Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var body=await r.Content.ReadAsStringAsync();
        Assert.Contains(Sprint2TestData.TeacherA.ToString(),body);
    }
    [Fact]
    public async Task TeacherDetailNonexistentReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/teachers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task SuperAdminCanViewForeignTeacher()
    {
        using var c=await Login("super");
        var rA=await c.GetAsync($"/api/v1/teachers/{Sprint2TestData.TeacherA}"); Assert.Equal(HttpStatusCode.OK,rA.StatusCode);
        var rB=await c.GetAsync($"/api/v1/teachers/{Sprint2TestData.TeacherB}"); Assert.Equal(HttpStatusCode.OK,rB.StatusCode);
        var list=await c.GetAsync("/api/v1/teachers"); Assert.Equal(HttpStatusCode.OK,list.StatusCode);
        var body=await list.Content.ReadAsStringAsync();
        Assert.Contains(Sprint2TestData.TeacherA.ToString(),body);
        Assert.Contains(Sprint2TestData.TeacherB.ToString(),body);
    }
    [Fact]
    public async Task SuperAdminCanMutateForeignTeacherSubject()
    {
        using var c=await Login("super");
        var assign=await c.PostAsync($"/api/v1/teachers/{Sprint2TestData.TeacherB}/subjects/{Sprint2TestData.Subject}",null);
        Assert.Equal(HttpStatusCode.Created,assign.StatusCode);
        var remove=await c.DeleteAsync($"/api/v1/teachers/{Sprint2TestData.TeacherB}/subjects/{Sprint2TestData.Subject}");
        Assert.Equal(HttpStatusCode.NoContent,remove.StatusCode);
    }
    [Fact]
    public async Task UnauthenticatedCannotAccessTeacherEndpoints()
    {
        using var c=factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/teachers")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync($"/api/v1/teachers/{Sprint2TestData.TeacherA}")).StatusCode);
    }
    [Fact]
    public async Task TeacherListPaginationRespectsPageSize()
    {
        using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(factory.ConnectionString).Options);
        var expectedFirstTeacherId=await db.Set<EgitimPlatform.Modules.Teachers.Entities.Teacher>()
            .AsNoTracking()
            .Where(x=>x.InstitutionId==Sprint2TestData.InstitutionA)
            .OrderBy(x=>x.LastName).ThenBy(x=>x.FirstName).ThenBy(x=>x.Id)
            .Select(x=>x.Id)
            .FirstAsync();
        using var c=await Login("admin");
        var r=await c.GetAsync("/api/v1/teachers?pageSize=1"); Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var body=await r.Content.ReadAsStringAsync();
        var arr=System.Text.Json.JsonDocument.Parse(body).RootElement;
        Assert.Equal(System.Text.Json.JsonValueKind.Array,arr.ValueKind);
        Assert.Equal(1,arr.GetArrayLength());
        Assert.Equal(expectedFirstTeacherId,arr[0].GetProperty("id").GetGuid());
    }
    [Theory]
    [InlineData("page=0")]
    [InlineData("page=100001")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=201")]
    public async Task TeacherListRejectsInvalidPagination(string query)
    {
        using var c=await Login("admin");
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync($"/api/v1/teachers?{query}")).StatusCode);
    }
    // ── Parent Endpoint Tests ──
    [Fact]
    public async Task ParentDetailSameInstitutionSucceeds()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/parents/{Sprint2TestData.ParentA}");
        Assert.Equal(HttpStatusCode.OK,r.StatusCode);
    }
    [Fact]
    public async Task ParentDetailCrossInstitutionReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/parents/{Sprint2TestData.ParentForeign}");
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task ParentDetailNonexistentReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/parents/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Theory]
    [InlineData("student",false)] [InlineData("parent",false)] [InlineData("teacher",false)] [InlineData("coach",false)]
    [InlineData("admin",true)] [InlineData("super",true)]
    public async Task ParentDetailEnforcesAuthorization(string user, bool allowed)
    {
        using var c=await Login(user); var r=await c.GetAsync($"/api/v1/parents/{Sprint2TestData.ParentA}");
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, r.StatusCode);
    }
    [Fact]
    public async Task ParentsByStudentReturnsCorrectRelationships()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/parents/by-student/{Sprint2TestData.StudentA}");
        Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var body=await r.Content.ReadAsStringAsync();
        Assert.Contains(Sprint2TestData.ParentA.ToString(),body);
        Assert.Contains("Guardian",body);
    }
    [Fact]
    public async Task ParentsByStudentCrossInstitutionReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/parents/by-student/{Sprint2TestData.StudentForeign}");
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task ParentsByStudentNonexistentReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.GetAsync($"/api/v1/parents/by-student/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task UnauthenticatedCannotAccessParentEndpoints()
    {
        using var c=factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync($"/api/v1/parents/{Sprint2TestData.ParentA}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync($"/api/v1/parents/by-student/{Sprint2TestData.StudentA}")).StatusCode);
    }
    // ── Teacher Subject Cross-Tenant Tests ──
    [Fact]
    public async Task TeacherSubjectCrossInstitutionTeacherFails()
    {
        using var c=await Login("admin");
        var r=await c.PostAsync($"/api/v1/teachers/{Sprint2TestData.TeacherB}/subjects/{Sprint2TestData.Subject}",null);
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task TeacherSubjectDuplicateAssignmentReturnsConflict()
    {
        using var c=await Login("admin");
        var r=await c.PostAsync($"/api/v1/teachers/{Sprint2TestData.TeacherA}/subjects/{Sprint2TestData.Subject}",null);
        Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);
    }
    // ── Parent ↔ Student Cross-Tenant Security Tests ──
    [Fact]
    public async Task ParentRelationshipCrossInstitutionStudentReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.PostAsJsonAsync("/api/v1/parents/relationships",new LinkParentCommand(Sprint2TestData.StudentForeign,Sprint2TestData.ParentA,"Guardian"));
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task SuperAdminCanManageForeignParentRelationship()
    {
        using var c=await Login("super");
        var detail=await c.GetAsync($"/api/v1/parents/{Sprint2TestData.ParentForeign}");
        Assert.Equal(HttpStatusCode.OK,detail.StatusCode);

        var list=await c.GetAsync($"/api/v1/parents/by-student/{Sprint2TestData.StudentForeign}");
        Assert.Equal(HttpStatusCode.OK,list.StatusCode);
        Assert.Contains(Sprint2TestData.LinkForeign.ToString(),await list.Content.ReadAsStringAsync());

        var deactivate=await c.PutAsJsonAsync($"/api/v1/parents/relationships/{Sprint2TestData.LinkForeign}",new SetParentRelationshipCommand(Sprint2TestData.LinkForeign,false));
        Assert.Equal(HttpStatusCode.OK,deactivate.StatusCode);
        var activate=await c.PutAsJsonAsync($"/api/v1/parents/relationships/{Sprint2TestData.LinkForeign}",new SetParentRelationshipCommand(Sprint2TestData.LinkForeign,true));
        Assert.Equal(HttpStatusCode.OK,activate.StatusCode);
    }
    [Fact]
    public async Task ParentRelationshipActivateNonexistentReturnsNotFound()
    {
        // Verifies institution-scoped fetch in SetActiveAsync
        using var c=await Login("admin");
        var fakeId=Guid.NewGuid();
        var r=await c.PutAsJsonAsync($"/api/v1/parents/relationships/{fakeId}",new SetParentRelationshipCommand(fakeId,true));
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task ParentRelationshipActivateForeignKnownLinkReturnsNotFound()
    {
        using var c=await Login("admin");
        var r=await c.PutAsJsonAsync($"/api/v1/parents/relationships/{Sprint2TestData.LinkForeign}",new SetParentRelationshipCommand(Sprint2TestData.LinkForeign,true));
        Assert.Equal(HttpStatusCode.NotFound,r.StatusCode);
    }
    [Fact]
    public async Task ParentRelationshipCannotActivateWithSoftDeletedParticipant()
    {
        using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(factory.ConnectionString).Options);
        var student=new Student { InstitutionId=Sprint2TestData.InstitutionA,FirstName="Deleted",LastName="Participant" };
        var parent=new Parent { InstitutionId=Sprint2TestData.InstitutionA,FirstName="Valid",LastName="Participant" };
        var link=new StudentParent { InstitutionId=Sprint2TestData.InstitutionA,StudentId=student.Id,ParentId=parent.Id,RelationshipType="Guardian",IsActive=false };
        db.AddRange(student,parent,link); await db.SaveChangesAsync();
        student.IsDeleted=true; student.DeletedAt=DateTimeOffset.UtcNow; await db.SaveChangesAsync();

        using var c=await Login("admin");
        var r=await c.PutAsJsonAsync($"/api/v1/parents/relationships/{link.Id}",new SetParentRelationshipCommand(link.Id,true));
        Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);
    }
    [Fact]
    public async Task ParentRelationshipCannotActivateDuplicateActiveRelation()
    {
        using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(factory.ConnectionString).Options);
        var inactive=new StudentParent {
            InstitutionId=Sprint2TestData.InstitutionA,StudentId=Sprint2TestData.StudentA,ParentId=Sprint2TestData.ParentA,
            RelationshipType="Guardian",IsActive=false
        };
        db.Add(inactive); await db.SaveChangesAsync();

        using var c=await Login("admin");
        var r=await c.PutAsJsonAsync($"/api/v1/parents/relationships/{inactive.Id}",new SetParentRelationshipCommand(inactive.Id,true));
        Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);
    }
}
