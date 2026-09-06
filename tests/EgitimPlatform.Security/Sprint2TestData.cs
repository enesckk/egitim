using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
namespace EgitimPlatform.Security;
public static class Sprint2TestData
{
    public static Guid InstitutionA, InstitutionB, StudentA, StudentOther, StudentForeign, GoalA, InactiveGoalOther, ParentA, ParentForeign, LinkA, TeacherA, Exam, Subject;
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>(); var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var a = new Institution { Name="Sprint2 A" }; var b = new Institution { Name="Sprint2 B" }; db.AddRange(a,b); await db.SaveChangesAsync(); InstitutionA=a.Id; InstitutionB=b.Id;
        var ids = new Dictionary<string, Guid>();
        foreach (var (name, role, inst) in new[] {
            ("student",Roles.Student,a.Id), ("other",Roles.Student,a.Id), ("foreign",Roles.Student,b.Id),
            ("new-parent",Roles.Parent,a.Id), ("new-teacher",Roles.Teacher,a.Id), ("parent",Roles.Parent,a.Id), ("unrelated-parent",Roles.Parent,a.Id), ("foreign-parent",Roles.Parent,b.Id),
            ("teacher",Roles.Teacher,a.Id), ("foreign-teacher",Roles.Teacher,b.Id),
            ("coach",Roles.Coach,a.Id), ("unassigned-coach",Roles.Coach,a.Id), ("foreign-coach",Roles.Coach,b.Id),
            ("admin",Roles.InstitutionAdmin,a.Id), ("foreign-admin",Roles.InstitutionAdmin,b.Id), ("super",Roles.SuperAdmin,a.Id) })
        {
            var u = new ApplicationUser { UserName=$"s2-{name}@test.local", Email=$"s2-{name}@test.local", FirstName=name, LastName="Test", InstitutionId=inst, IsActive=true, EmailConfirmed=true };
            var created = await users.CreateAsync(u,"Test@12345"); if (!created.Succeeded) throw new InvalidOperationException("Test user creation failed.");
            var assigned = await users.AddToRoleAsync(u,role); if (!assigned.Succeeded) throw new InvalidOperationException("Test role assignment failed."); ids[name]=u.Id;
        }
        var sa = new Student { InstitutionId=a.Id, UserId=ids["student"], FirstName="Student", LastName="A" };
        var so = new Student { InstitutionId=a.Id, UserId=ids["other"], FirstName="Student", LastName="Other" };
        var sb = new Student { InstitutionId=b.Id, UserId=ids["foreign"], FirstName="Student", LastName="Foreign" }; db.AddRange(sa,so,sb); StudentA=sa.Id; StudentOther=so.Id; StudentForeign=sb.Id;
        var pa = new Parent { InstitutionId=a.Id, UserId=ids["parent"], FirstName="Related", LastName="Parent", Phone="PRIVATE-PHONE" };
        var pb = new Parent { InstitutionId=b.Id, UserId=ids["foreign-parent"], FirstName="Foreign", LastName="Parent" }; db.AddRange(pa,pb); ParentA=pa.Id; ParentForeign=pb.Id;
        var link = new StudentParent { InstitutionId=a.Id, StudentId=sa.Id, ParentId=pa.Id, RelationshipType="Guardian", Notes="PRIVATE-RELATIONSHIP-NOTE" }; db.Add(link); LinkA=link.Id;
        var coach = new Coach { InstitutionId=a.Id, UserId=ids["coach"], FirstName="Coach", LastName="Assigned" }; db.Add(coach);
        db.Add(new Coach { InstitutionId=a.Id, UserId=ids["unassigned-coach"], FirstName="Coach", LastName="Unassigned" });
        db.Add(new Coach { InstitutionId=b.Id, UserId=ids["foreign-coach"], FirstName="Coach", LastName="Foreign" });
        db.Add(new StudentCoachAssignment { InstitutionId=a.Id, StudentId=sa.Id, CoachId=coach.Id, IsActive=true, IsPrimary=true });
        var exam = new ExamType { Code="TEST-S2-EXAM", Name="Synthetic test exam" }; db.Add(exam); Exam=exam.Id;
        var subject = new Subject { Code="TEST-S2-SUBJECT", Name="Synthetic test subject", ExamTypeId=exam.Id }; db.Add(subject); Subject=subject.Id;
        var teacher = new Teacher { InstitutionId=a.Id, UserId=ids["teacher"], FirstName="Teacher", LastName="Test" }; db.Add(teacher); TeacherA=teacher.Id;
        db.Add(new TeacherSubject { InstitutionId=a.Id, TeacherId=teacher.Id, SubjectId=subject.Id });
        var goal = new StudentGoal { InstitutionId=a.Id, StudentId=sa.Id, Title="Current goal", Description="PRIVATE-GOAL-NOTE", TargetExamTypeId=exam.Id }; db.Add(goal); GoalA=goal.Id;
        var inactive = new StudentGoal { InstitutionId=a.Id, StudentId=so.Id, Title="Inactive", IsActive=false }; db.Add(inactive); InactiveGoalOther=inactive.Id;
        db.Add(EgitimPlatform.Modules.Students.Services.GoalHistory.Capture(goal,"Created",ids["student"]));
        await db.SaveChangesAsync();
    }
}
