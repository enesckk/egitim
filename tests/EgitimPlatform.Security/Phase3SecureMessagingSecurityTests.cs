using System.Security;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Communication.Features.ManageMessages;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Phase3SecureMessagingSecurityTests
{
    private ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Coach_Can_Message_Assigned_Student_Successfully()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        var (coach, student) = await SetupActiveCoachStudentAsync(db, instId, coachUserId, studentUserId);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Coach]);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        var result = await handler.SendMessageAsync(new SendMessageRequest(
            null,
            studentUserId,
            "Merhaba öğrenci, haftalık ödevlerini tamamla.",
            "Ödev Takibi"
        ));

        Assert.NotNull(result);
        Assert.Equal("Merhaba öğrenci, haftalık ödevlerini tamamla.", result.Content);

        // Verify conversation was created
        var conv = await db.Set<Conversation>().FirstOrDefaultAsync(c => c.Id == result.ConversationId);
        Assert.NotNull(conv);
        Assert.Equal(instId, conv.InstitutionId);

        // Verify message was created
        var msg = await db.Set<Message>().FirstOrDefaultAsync(m => m.Id == result.Id);
        Assert.NotNull(msg);
        Assert.False(msg.IsRead);
    }

    [Fact]
    public async Task Coach_Cannot_Message_Unassigned_Student_FailsClosed()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        // Coach and student in same institution, but NO active assignment
        await CreateUserWithRoleAsync(db, instId, coachUserId, Roles.Coach);
        await CreateUserWithRoleAsync(db, instId, studentUserId, Roles.Student);

        var coach = new Coach { Id = Guid.NewGuid(), InstitutionId = instId, UserId = coachUserId, FirstName = "Coach", LastName = "User" };
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, UserId = studentUserId, FirstName = "Student", LastName = "User" };
        db.Set<Coach>().Add(coach);
        db.Set<Student>().Add(student);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Coach]);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<SecurityException>(() => handler.SendMessageAsync(new SendMessageRequest(
            null,
            studentUserId,
            "İzinsiz mesaj denemesi",
            null
        )));
    }

    [Fact]
    public async Task Cross_Tenant_Messaging_Is_Strictly_Blocked()
    {
        using var db = CreateDb();
        var instA = Guid.NewGuid();
        var instB = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        // Coach in Inst A, Student in Inst B
        await CreateUserWithRoleAsync(db, instA, coachUserId, Roles.Coach);
        await CreateUserWithRoleAsync(db, instB, studentUserId, Roles.Student);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instA);
        currentUser.Roles.Returns([Roles.Coach]);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<SecurityException>(() => handler.SendMessageAsync(new SendMessageRequest(
            null,
            studentUserId,
            "Cross-tenant attempt",
            null
        )));
    }

    [Fact]
    public async Task Teacher_Can_Message_Student_In_Assigned_Classroom()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        await CreateUserWithRoleAsync(db, instId, teacherUserId, Roles.Teacher);
        await CreateUserWithRoleAsync(db, instId, studentUserId, Roles.Student);

        var teacher = new Teacher { Id = Guid.NewGuid(), InstitutionId = instId, UserId = teacherUserId, FirstName = "Teacher", LastName = "User" };
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, UserId = studentUserId, FirstName = "Student", LastName = "User" };
        var classroom = new Classroom { Id = Guid.NewGuid(), InstitutionId = instId, Name = "10-B" };
        db.Set<Teacher>().Add(teacher);
        db.Set<Student>().Add(student);
        db.Set<Classroom>().Add(classroom);

        db.Set<ClassroomTeacher>().Add(new ClassroomTeacher { Id = Guid.NewGuid(), InstitutionId = instId, TeacherId = teacher.Id, ClassroomId = classroom.Id });
        db.Set<ClassroomStudent>().Add(new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instId, StudentId = student.Id, ClassroomId = classroom.Id });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(teacherUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Teacher]);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        var result = await handler.SendMessageAsync(new SendMessageRequest(
            null,
            studentUserId,
            "Ders notları yüklendi.",
            null
        ));

        Assert.NotNull(result);
        Assert.Equal("Ders notları yüklendi.", result.Content);
    }

    [Fact]
    public async Task Teacher_Cannot_Message_Student_Outside_Assigned_Classroom()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        await CreateUserWithRoleAsync(db, instId, teacherUserId, Roles.Teacher);
        await CreateUserWithRoleAsync(db, instId, studentUserId, Roles.Student);

        var teacher = new Teacher { Id = Guid.NewGuid(), InstitutionId = instId, UserId = teacherUserId, FirstName = "Teacher", LastName = "User" };
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, UserId = studentUserId, FirstName = "Student", LastName = "User" };
        db.Set<Teacher>().Add(teacher);
        db.Set<Student>().Add(student);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(teacherUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Teacher]);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<SecurityException>(() => handler.SendMessageAsync(new SendMessageRequest(
            null,
            studentUserId,
            "Sınıf dışı mesaj denemesi",
            null
        )));
    }

    [Fact]
    public async Task Parent_Can_Message_Assigned_Coach_Of_Their_Child()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var parentUserId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        await CreateUserWithRoleAsync(db, instId, parentUserId, Roles.Parent);
        await CreateUserWithRoleAsync(db, instId, coachUserId, Roles.Coach);
        await CreateUserWithRoleAsync(db, instId, studentUserId, Roles.Student);

        var parent = new Parent { Id = Guid.NewGuid(), InstitutionId = instId, UserId = parentUserId, FirstName = "Parent", LastName = "User" };
        var coach = new Coach { Id = Guid.NewGuid(), InstitutionId = instId, UserId = coachUserId, FirstName = "Coach", LastName = "User" };
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, UserId = studentUserId, FirstName = "Student", LastName = "User" };

        db.Set<Parent>().Add(parent);
        db.Set<Coach>().Add(coach);
        db.Set<Student>().Add(student);

        db.Set<StudentParent>().Add(new StudentParent { Id = Guid.NewGuid(), InstitutionId = instId, ParentId = parent.Id, StudentId = student.Id });
        db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment { Id = Guid.NewGuid(), InstitutionId = instId, CoachId = coach.Id, StudentId = student.Id, IsActive = true, AssignedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(parentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Parent]);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        var result = await handler.SendMessageAsync(new SendMessageRequest(
            null,
            coachUserId,
            "Haftalık gelişim raporunu aldım, teşekkürler.",
            null
        ));

        Assert.NotNull(result);
        Assert.Equal("Haftalık gelişim raporunu aldım, teşekkürler.", result.Content);
    }

    [Fact]
    public async Task Non_Participant_Cannot_Read_Conversation_History()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var eavesdropperId = Guid.NewGuid();

        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            Participant1Id = p1,
            Participant2Id = p2,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<Conversation>().Add(conv);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(eavesdropperId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsSuperAdmin.Returns(false);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<SecurityException>(() => handler.GetConversationMessagesAsync(conv.Id));
    }

    private static async Task<(Coach Coach, Student Student)> SetupActiveCoachStudentAsync(
        ApplicationDbContext db, Guid instId, Guid coachUserId, Guid studentUserId)
    {
        await CreateUserWithRoleAsync(db, instId, coachUserId, Roles.Coach);
        await CreateUserWithRoleAsync(db, instId, studentUserId, Roles.Student);

        var coach = new Coach { Id = Guid.NewGuid(), InstitutionId = instId, UserId = coachUserId, FirstName = "Coach", LastName = "User" };
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, UserId = studentUserId, FirstName = "Student", LastName = "User" };

        db.Set<Coach>().Add(coach);
        db.Set<Student>().Add(student);

        db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            CoachId = coach.Id,
            StudentId = student.Id,
            IsActive = true,
            AssignedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
        return (coach, student);
    }

    private static async Task CreateUserWithRoleAsync(ApplicationDbContext db, Guid instId, Guid userId, string roleName)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            InstitutionId = instId,
            Email = $"{roleName.ToLower()}_{userId:N}@test.local",
            UserName = $"{roleName.ToLower()}_{userId:N}@test.local",
            FirstName = roleName,
            LastName = "User",
            IsActive = true
        };
        db.Set<ApplicationUser>().Add(user);

        var role = await db.Set<ApplicationRole>().FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            role = new ApplicationRole { Id = Guid.NewGuid(), Name = roleName, NormalizedName = roleName.ToUpper() };
            db.Set<ApplicationRole>().Add(role);
        }

        db.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid>
        {
            UserId = userId,
            RoleId = role.Id
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Parent_Cannot_Message_Unrelated_Coach_FailsClosed()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var parentUserId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        await CreateUserWithRoleAsync(db, instId, parentUserId, Roles.Parent);
        await CreateUserWithRoleAsync(db, instId, coachUserId, Roles.Coach);
        await CreateUserWithRoleAsync(db, instId, studentUserId, Roles.Student);

        var parent = new Parent { Id = Guid.NewGuid(), InstitutionId = instId, UserId = parentUserId, FirstName = "Parent", LastName = "User" };
        var coach = new Coach { Id = Guid.NewGuid(), InstitutionId = instId, UserId = coachUserId, FirstName = "Coach", LastName = "User" };
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, UserId = studentUserId, FirstName = "Student", LastName = "User" };

        db.Set<Parent>().Add(parent);
        db.Set<Coach>().Add(coach);
        db.Set<Student>().Add(student);

        // Parent is linked to student, BUT coach is NOT assigned to student
        db.Set<StudentParent>().Add(new StudentParent { Id = Guid.NewGuid(), InstitutionId = instId, ParentId = parent.Id, StudentId = student.Id });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(parentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Parent]);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<SecurityException>(() => handler.SendMessageAsync(new SendMessageRequest(
            null,
            coachUserId,
            "İlişkisiz koça yetkisiz mesaj",
            null
        )));
    }

    [Fact]
    public async Task Parent_Cannot_Message_Teacher_Whose_Class_Child_Is_Not_In()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var parentUserId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        await CreateUserWithRoleAsync(db, instId, parentUserId, Roles.Parent);
        await CreateUserWithRoleAsync(db, instId, teacherUserId, Roles.Teacher);
        await CreateUserWithRoleAsync(db, instId, studentUserId, Roles.Student);

        var parent = new Parent { Id = Guid.NewGuid(), InstitutionId = instId, UserId = parentUserId, FirstName = "Parent", LastName = "User" };
        var teacher = new Teacher { Id = Guid.NewGuid(), InstitutionId = instId, UserId = teacherUserId, FirstName = "Teacher", LastName = "User" };
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, UserId = studentUserId, FirstName = "Student", LastName = "User" };

        db.Set<Parent>().Add(parent);
        db.Set<Teacher>().Add(teacher);
        db.Set<Student>().Add(student);

        db.Set<StudentParent>().Add(new StudentParent { Id = Guid.NewGuid(), InstitutionId = instId, ParentId = parent.Id, StudentId = student.Id });
        // Teacher is in Classroom 11-A, student is NOT in Classroom 11-A
        var classroom = new Classroom { Id = Guid.NewGuid(), InstitutionId = instId, Name = "11-A" };
        db.Set<Classroom>().Add(classroom);
        db.Set<ClassroomTeacher>().Add(new ClassroomTeacher { Id = Guid.NewGuid(), InstitutionId = instId, TeacherId = teacher.Id, ClassroomId = classroom.Id });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(parentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Parent]);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<SecurityException>(() => handler.SendMessageAsync(new SendMessageRequest(
            null,
            teacherUserId,
            "Yetkisiz öğretmen mesajı",
            null
        )));
    }

    [Fact]
    public async Task Revoked_Coach_Assignment_Blocks_Future_Messages()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        var (coach, student) = await SetupActiveCoachStudentAsync(db, instId, coachUserId, studentUserId);

        // Revoke assignment
        var assignment = await db.Set<StudentCoachAssignment>().FirstAsync(a => a.CoachId == coach.Id && a.StudentId == student.Id);
        assignment.IsActive = false;
        assignment.EndedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Coach]);

        var handler = new MessagesHandler(db, currentUser, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<SecurityException>(() => handler.SendMessageAsync(new SendMessageRequest(
            null,
            studentUserId,
            "Görev süresi bittikten sonra mesaj",
            null
        )));
    }
}
