using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Content.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Notebook.Entities;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Questions.Constants;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EgitimPlatform.Api.Seeding;

/// <summary>
/// Dedicated seeder for the DEMO / Showcase V1 dataset.
/// Operates ONLY when DemoSeed:Enabled is explicitly configured true.
/// Completely idempotent using deterministic Guid markers (DEMO_SHOWCASE_V1).
/// </summary>
public class DemoDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly DemoSeedSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DemoDataSeeder> _logger;

    // Stable Deterministic GUID Identifiers (DEMO_SHOWCASE_V1)
    public static readonly Guid DemoInstitutionId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid DemoAdminUserId = Guid.Parse("d0000000-0000-0000-0000-000000000002");
    public static readonly Guid DemoTeacherUserId = Guid.Parse("d0000000-0000-0000-0000-000000000003");
    public static readonly Guid DemoCoachUserId = Guid.Parse("d0000000-0000-0000-0000-000000000004");
    public static readonly Guid DemoStudentUserId = Guid.Parse("d0000000-0000-0000-0000-000000000005");
    public static readonly Guid DemoParentUserId = Guid.Parse("d0000000-0000-0000-0000-000000000006");

    public static readonly Guid DemoStudentId = Guid.Parse("d0000000-0000-0000-0000-000000000007");
    public static readonly Guid DemoParentEntityId = Guid.Parse("d0000000-0000-0000-0000-000000000008");
    public static readonly Guid DemoTeacherEntityId = Guid.Parse("d0000000-0000-0000-0000-000000000009");
    public static readonly Guid DemoCoachEntityId = Guid.Parse("d0000000-0000-0000-0000-00000000000a");
    public static readonly Guid DemoStudent2Id = Guid.Parse("d0000000-0000-0000-0000-00000000000d");
    public static readonly Guid DemoClassroom12AId = Guid.Parse("d0000000-0000-0000-0000-000000000060");
    public static readonly Guid DemoClassroom12BId = Guid.Parse("d0000000-0000-0000-0000-000000000061");
    public static readonly Guid DemoClassroom12CId = Guid.Parse("d0000000-0000-0000-0000-000000000067");
    public static readonly Guid DemoStudyPlanId = Guid.Parse("d0000000-0000-0000-0000-000000000070");
    public static readonly Guid DemoStudyPlanS9Id = Guid.Parse("d0000000-0000-0000-0000-000000000090");

    // Phase 2 Deterministic Identifiers
    public static readonly Guid DemoMaterial1Id = Guid.Parse("d0000000-0000-0040-0000-000000000001");
    public static readonly Guid DemoMaterial2Id = Guid.Parse("d0000000-0000-0040-0000-000000000002");
    public static readonly Guid DemoMaterialAssignment1Id = Guid.Parse("d0000000-0000-0041-0000-000000000001");
    public static readonly Guid DemoMaterialAssignment2Id = Guid.Parse("d0000000-0000-0041-0000-000000000002");
    public static readonly Guid DemoMaterialAssignment3Id = Guid.Parse("d0000000-0000-0041-0000-000000000003");
    public static readonly Guid DemoMaterialAssignment4Id = Guid.Parse("d0000000-0000-0041-0000-000000000004");

    public static readonly Guid DemoMeeting1Id = Guid.Parse("d0000000-0000-0050-0000-000000000001");
    public static readonly Guid DemoMeeting2Id = Guid.Parse("d0000000-0000-0050-0000-000000000002");
    public static readonly Guid DemoMeeting3Id = Guid.Parse("d0000000-0000-0050-0000-000000000003");
    public static readonly Guid DemoMeeting4Id = Guid.Parse("d0000000-0000-0050-0000-000000000004");
    public static readonly Guid DemoMeeting5Id = Guid.Parse("d0000000-0000-0050-0000-000000000005");

    public static readonly Guid DemoConvCoachStudent1Id = Guid.Parse("d0000000-0000-0060-0000-000000000001");
    public static readonly Guid DemoConvCoachParent1Id = Guid.Parse("d0000000-0000-0060-0000-000000000002");
    public static readonly Guid DemoConvTeacherStudent1Id = Guid.Parse("d0000000-0000-0060-0000-000000000003");
    public static readonly Guid DemoConvTeacherParent1Id = Guid.Parse("d0000000-0000-0060-0000-000000000004");
    public static readonly Guid DemoConvCoachStudent9Id = Guid.Parse("d0000000-0000-0060-0000-000000000005");

    public static Guid DemoStudentUserGuid(int s) =>
        s == 1 ? DemoStudentUserId : Guid.Parse($"d0000000-0000-0010-0000-{s:D12}");

    public static Guid DemoStudentEntityGuid(int s) =>
        s == 1 ? DemoStudentId : s == 2 ? DemoStudent2Id : Guid.Parse($"d0000000-0000-0011-0000-{s:D12}");

    public static Guid DemoTeacherUserGuid(int t) =>
        t == 1 ? DemoTeacherUserId : Guid.Parse($"d0000000-0000-0012-0000-{t:D12}");

    public static Guid DemoTeacherEntityGuid(int t) =>
        t == 1 ? DemoTeacherEntityId : Guid.Parse($"d0000000-0000-0013-0000-{t:D12}");

    public static Guid DemoCoachUserGuid(int c) =>
        c == 1 ? DemoCoachUserId : Guid.Parse($"d0000000-0000-0014-0000-{c:D12}");

    public static Guid DemoCoachEntityGuid(int c) =>
        c == 1 ? DemoCoachEntityId : Guid.Parse($"d0000000-0000-0015-0000-{c:D12}");

    public static Guid DemoParentUserGuid(int p) =>
        p == 1 ? DemoParentUserId : Guid.Parse($"d0000000-0000-0016-0000-{p:D12}");

    public static Guid DemoParentEntityGuid(int p) =>
        p == 1 ? DemoParentEntityId : Guid.Parse($"d0000000-0000-0017-0000-{p:D12}");

    public static Guid DemoAttemptGuid(int s, int e) =>
        s == 1 && e <= 5
            ? Guid.Parse($"d0000000-0000-0000-0000-00000000002{e}")
            : Guid.Parse($"d0000000-0000-0020-{s:D4}-{e:D12}");

    public static Guid DemoSubjectResultGuid(int s, int e, int secOrder) =>
        Guid.Parse($"d0000000-0000-0021-{s:D4}-00000000{e:D2}{secOrder:D2}");

    public static Guid DemoTopicResultGuid(int s, int e, int topIdx) =>
        Guid.Parse($"d0000000-0000-0022-{s:D4}-00000000{e:D2}{topIdx:D2}");

    public DemoDataSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<DemoSeedSettings> settings,
        IWebHostEnvironment environment,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Demo seed is disabled (DemoSeed:Enabled = false). Skipping.");
            return;
        }

        if (_environment.IsProduction() && !_settings.Enabled)
        {
            _logger.LogWarning("Demo seed is blocked in Production environment when not explicitly enabled.");
            return;
        }

        ValidateConfiguration();

        // Check idempotency via Demo Institution existence
        var existingInst = await _db.Set<Institution>().IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == DemoInstitutionId, ct);
        if (existingInst != null)
        {
            _logger.LogInformation("Demo showcase dataset (DEMO_SHOWCASE_V1) already exists. Skipping duplicate base seeding; applying idempotent enrichment.");
            await SeedShowcaseEnrichmentAsync(ct);
            return;
        }

        _logger.LogInformation("Seeding Demo Showcase Dataset (DEMO_SHOWCASE_V1)...");

        // 1. Demo Institution
        var institution = new Institution
        {
            Id = DemoInstitutionId,
            Name = "Demo Eğitim Kurumu",
            IsActive = true
        };
        _db.Set<Institution>().Add(institution);
        await _db.SaveChangesAsync(ct);

        // 2. Identity Users & Roles
        await SeedRolesAsync(ct);
        await SeedUsersAsync(ct);

        // 3. Domain Entities
        await SeedDomainProfilesAsync(ct);

        // 4. Taxonomy & Questions
        var taxonomy = await SeedTaxonomyAsync(ct);
        var questions = await SeedQuestionsAsync(taxonomy, ct);

        // 5. Hierarchy (Classrooms, Teachers, Coaches, Parents, Students)
        await SeedExpandedHierarchyAsync(taxonomy, ct);

        // 6. 10 Chronological Demo Exams & Consistent Attempts (Phase 1)
        await SeedExpandedExamsAndAttemptsAsync(taxonomy, ct);

        // 7. Historical Coach Task
        await SeedCoachTasksAsync(ct);

        // 8. Notifications
        await SeedNotificationsAsync(ct);

        // 9. Linked question-level data, study plan and notes
        await SeedShowcaseEnrichmentAsync(ct);

        _logger.LogInformation("Demo Showcase Dataset (DEMO_SHOWCASE_V1) successfully seeded.");
    }

    public async Task ResetDemoDataAsync(CancellationToken ct = default)
    {
        if (_environment.IsProduction())
        {
            throw new InvalidOperationException("Demo dataset reset is strictly forbidden in Production.");
        }

        _logger.LogInformation("Resetting Demo Showcase Dataset (DEMO_SHOWCASE_V1)...");

        // Safe removal restricted exclusively to DemoInstitutionId and deterministic Demo IDs
        var topicResults = await _db.Set<StudentExamTopicResult>().IgnoreQueryFilters().Where(r => r.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudentExamTopicResult>().RemoveRange(topicResults);

        var subjectResults = await _db.Set<StudentExamSubjectResult>().IgnoreQueryFilters().Where(r => r.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudentExamSubjectResult>().RemoveRange(subjectResults);

        var questionResults = await _db.Set<StudentExamQuestionResult>().IgnoreQueryFilters().Where(r => r.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudentExamQuestionResult>().RemoveRange(questionResults);

        var examQuestions = await _db.Set<ExamQuestion>().IgnoreQueryFilters().Where(q => q.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<ExamQuestion>().RemoveRange(examQuestions);

        var attempts = await _db.Set<StudentExamAttempt>().IgnoreQueryFilters().Where(a => a.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudentExamAttempt>().RemoveRange(attempts);

        var exams = await _db.Set<Exam>().IgnoreQueryFilters().Include(e => e.Sections).Where(e => e.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        foreach (var exam in exams)
        {
            _db.Set<ExamSection>().RemoveRange(exam.Sections);
        }
        _db.Set<Exam>().RemoveRange(exams);

        var studyTasks = await _db.Set<StudyTask>().IgnoreQueryFilters().Where(t => t.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudyTask>().RemoveRange(studyTasks);

        var studyPlans = await _db.Set<StudyPlan>().IgnoreQueryFilters().Where(p => p.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudyPlan>().RemoveRange(studyPlans);

        var notebookEntries = await _db.Set<NotebookEntry>().IgnoreQueryFilters().Where(n => n.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<NotebookEntry>().RemoveRange(notebookEntries);

        var coachNotes = await _db.Set<CoachPrivateNote>().IgnoreQueryFilters().Where(n => n.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<CoachPrivateNote>().RemoveRange(coachNotes);

        var assignments = await _db.Set<ContentMaterialAssignment>().IgnoreQueryFilters().Where(a => a.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<ContentMaterialAssignment>().RemoveRange(assignments);

        var materials = await _db.Set<ContentMaterial>().IgnoreQueryFilters().Where(m => m.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<ContentMaterial>().RemoveRange(materials);

        var meetings = await _db.Set<CoachMeeting>().IgnoreQueryFilters().Where(m => m.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<CoachMeeting>().RemoveRange(meetings);

        var messages = await _db.Set<Message>().IgnoreQueryFilters().Where(m => m.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Message>().RemoveRange(messages);

        var conversations = await _db.Set<Conversation>().IgnoreQueryFilters().Where(c => c.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Conversation>().RemoveRange(conversations);

        var classroomStudents = await _db.Set<ClassroomStudent>().IgnoreQueryFilters().Where(c => c.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<ClassroomStudent>().RemoveRange(classroomStudents);

        var classroomTeachers = await _db.Set<ClassroomTeacher>().IgnoreQueryFilters().Where(c => c.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<ClassroomTeacher>().RemoveRange(classroomTeachers);

        var classrooms = await _db.Set<Classroom>().IgnoreQueryFilters().Where(c => c.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Classroom>().RemoveRange(classrooms);

        var teacherSubjects = await _db.Set<TeacherSubject>().IgnoreQueryFilters().Where(t => t.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<TeacherSubject>().RemoveRange(teacherSubjects);

        var demoQuestionIds = await _db.Set<Question>().IgnoreQueryFilters().Where(q => q.InstitutionId == DemoInstitutionId).Select(q => q.Id).ToListAsync(ct);
        var styleAssignments = await _db.Set<QuestionStyleAssignment>().Where(a => demoQuestionIds.Contains(a.QuestionId)).ToListAsync(ct);
        _db.Set<QuestionStyleAssignment>().RemoveRange(styleAssignments);

        var tasks = await _db.Set<CoachTask>().IgnoreQueryFilters().Where(t => t.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<CoachTask>().RemoveRange(tasks);

        var notifications = await _db.Set<Notification>().IgnoreQueryFilters().Where(n => n.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Notification>().RemoveRange(notifications);

        var studentParents = await _db.Set<StudentParent>().IgnoreQueryFilters().Where(sp => sp.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudentParent>().RemoveRange(studentParents);

        var coachAssignments = await _db.Set<StudentCoachAssignment>().IgnoreQueryFilters().Where(ca => ca.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudentCoachAssignment>().RemoveRange(coachAssignments);

        var students = await _db.Set<Student>().IgnoreQueryFilters().Where(s => s.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Student>().RemoveRange(students);

        var parents = await _db.Set<Parent>().IgnoreQueryFilters().Where(p => p.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Parent>().RemoveRange(parents);

        var coaches = await _db.Set<Coach>().IgnoreQueryFilters().Where(c => c.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Coach>().RemoveRange(coaches);

        var teachers = await _db.Set<Teacher>().IgnoreQueryFilters().Where(t => t.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Teacher>().RemoveRange(teachers);

        var inst = await _db.Set<Institution>().IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == DemoInstitutionId, ct);
        if (inst != null) _db.Set<Institution>().Remove(inst);

        // Delete all users belonging to DemoInstitutionId
        var demoUsers = await _userManager.Users.Where(u => u.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        foreach (var user in demoUsers)
        {
            await _userManager.DeleteAsync(user);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Demo dataset cleared. Re-seeding showcase data...");

        await SeedAsync(ct);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_settings.AdminPassword) ||
            string.IsNullOrWhiteSpace(_settings.TeacherPassword) ||
            string.IsNullOrWhiteSpace(_settings.CoachPassword) ||
            string.IsNullOrWhiteSpace(_settings.StudentPassword) ||
            string.IsNullOrWhiteSpace(_settings.ParentPassword))
        {
            throw new InvalidOperationException(
                "DemoSeed is enabled but required password configuration is missing. " +
                "Provide AdminPassword, TeacherPassword, CoachPassword, StudentPassword, ParentPassword in DemoSeed configuration.");
        }
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        foreach (var roleName in Roles.All)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = roleName, Description = $"{roleName} role" });
            }
        }
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        var usersToCreate = new (Guid Id, string Email, string Name, string Role, string Password)[]
        {
            (DemoAdminUserId, _settings.AdminEmail, "Demo Yönetici", Roles.InstitutionAdmin, _settings.AdminPassword!),
            (DemoTeacherUserId, _settings.TeacherEmail, "Ayşe Öğretmen", Roles.Teacher, _settings.TeacherPassword!),
            (DemoCoachUserId, _settings.CoachEmail, "Mehmet Koç", Roles.Coach, _settings.CoachPassword!),
            (DemoStudentUserId, _settings.StudentEmail, "Ahmet Yılmaz", Roles.Student, _settings.StudentPassword!),
            (DemoParentUserId, _settings.ParentEmail, "Fatma Yılmaz", Roles.Parent, _settings.ParentPassword!)
        };

        foreach (var u in usersToCreate)
        {
            var existing = await _userManager.FindByEmailAsync(u.Email);
            if (existing == null)
            {
                var user = new ApplicationUser
                {
                    Id = u.Id,
                    UserName = u.Email,
                    Email = u.Email,
                    EmailConfirmed = true,
                    InstitutionId = DemoInstitutionId,
                    IsActive = true
                };

                var res = await _userManager.CreateAsync(user, u.Password);
                if (res.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, u.Role);
                }
                else
                {
                    _logger.LogError("Failed to create demo user {Email}: {Errors}", u.Email, string.Join(", ", res.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedDomainProfilesAsync(CancellationToken ct)
    {
        // Student
        if (!await _db.Set<Student>().IgnoreQueryFilters().AnyAsync(s => s.Id == DemoStudentId, ct))
        {
            _db.Set<Student>().Add(new Student
            {
                Id = DemoStudentId,
                InstitutionId = DemoInstitutionId,
                UserId = DemoStudentUserId,
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                StudentNumber = "1201",
                GradeLevel = 12,
                SchoolName = "Demo Anadolu Lisesi",
                EnrollmentDate = DateTimeOffset.UtcNow.AddMonths(-6),
                Status = StudentStatus.Active
            });
        }

        // Parent
        if (!await _db.Set<Parent>().IgnoreQueryFilters().AnyAsync(p => p.Id == DemoParentEntityId, ct))
        {
            _db.Set<Parent>().Add(new Parent
            {
                Id = DemoParentEntityId,
                InstitutionId = DemoInstitutionId,
                UserId = DemoParentUserId,
                FirstName = "Fatma",
                LastName = "Yılmaz",
                Email = _settings.ParentEmail,
                Phone = "5550001122"
            });
        }

        // StudentParent Link
        if (!await _db.Set<StudentParent>().IgnoreQueryFilters().AnyAsync(sp => sp.StudentId == DemoStudentId && sp.ParentId == DemoParentEntityId, ct))
        {
            _db.Set<StudentParent>().Add(new StudentParent
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-00000000000b"),
                InstitutionId = DemoInstitutionId,
                StudentId = DemoStudentId,
                ParentId = DemoParentEntityId,
                RelationshipType = "Mother",
                IsActive = true
            });
        }

        // Coach
        if (!await _db.Set<Coach>().IgnoreQueryFilters().AnyAsync(c => c.Id == DemoCoachEntityId, ct))
        {
            _db.Set<Coach>().Add(new Coach
            {
                Id = DemoCoachEntityId,
                InstitutionId = DemoInstitutionId,
                UserId = DemoCoachUserId,
                FirstName = "Mehmet",
                LastName = "Koç"
            });
        }

        // StudentCoachAssignment
        if (!await _db.Set<StudentCoachAssignment>().IgnoreQueryFilters().AnyAsync(ca => ca.StudentId == DemoStudentId && ca.CoachId == DemoCoachEntityId, ct))
        {
            _db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-00000000000c"),
                InstitutionId = DemoInstitutionId,
                StudentId = DemoStudentId,
                CoachId = DemoCoachEntityId,
                IsPrimary = true,
                IsActive = true,
                AssignedAt = DateTimeOffset.UtcNow.AddMonths(-3)
            });
        }

        // Teacher
        if (!await _db.Set<Teacher>().IgnoreQueryFilters().AnyAsync(t => t.Id == DemoTeacherEntityId, ct))
        {
            _db.Set<Teacher>().Add(new Teacher
            {
                Id = DemoTeacherEntityId,
                InstitutionId = DemoInstitutionId,
                UserId = DemoTeacherUserId,
                FirstName = "Ayşe",
                LastName = "Öğretmen",
                Title = "Matematik Öğretmeni"
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<DemoTaxonomyContainer> SeedTaxonomyAsync(CancellationToken ct)
    {
        var examType = await _db.Set<ExamType>().IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Code == "TYT-DEMO" || e.Code == "TYT", ct);
        if (examType == null)
        {
            examType = new ExamType { Id = Guid.Parse("d0000000-0000-0000-0000-000000000030"), Code = "TYT-DEMO", Name = "TYT" };
            _db.Set<ExamType>().Add(examType);
            await _db.SaveChangesAsync(ct);
        }

        // Subjects
        var subMat = await GetOrCreateSubjectAsync(examType.Id, "MAT-DEMO", "Matematik", ct);
        var subFiz = await GetOrCreateSubjectAsync(examType.Id, "FIZ-DEMO", "Fizik", ct);
        var subTur = await GetOrCreateSubjectAsync(examType.Id, "TUR-DEMO", "Türkçe", ct);
        var subKim = await GetOrCreateSubjectAsync(examType.Id, "KIM-DEMO", "Kimya", ct);
        var subBiy = await GetOrCreateSubjectAsync(examType.Id, "BIY-DEMO", "Biyoloji", ct);

        // Topics
        var topMatTk = await GetOrCreateTopicAsync(subMat.Id, "MAT-TK", "Temel Kavramlar", ct);
        var topMatPrb = await GetOrCreateTopicAsync(subMat.Id, "MAT-PRB", "Problemler", ct);
        var topMatFonk = await GetOrCreateTopicAsync(subMat.Id, "MAT-FONK", "Fonksiyonlar", ct);
        var topMatTrig = await GetOrCreateTopicAsync(subMat.Id, "MAT-TRIG", "Trigonometri", ct);

        var topFizElek = await GetOrCreateTopicAsync(subFiz.Id, "FIZ-ELEK", "Elektrik", ct);
        var topFizHk = await GetOrCreateTopicAsync(subFiz.Id, "FIZ-HK", "Hareket ve Kuvvet", ct);

        var topTurPar = await GetOrCreateTopicAsync(subTur.Id, "TUR-PAR", "Paragraf", ct);
        var topTurDbg = await GetOrCreateTopicAsync(subTur.Id, "TUR-DBG", "Dil Bilgisi", ct);

        var topKimKt = await GetOrCreateTopicAsync(subKim.Id, "KIM-KT", "Kimyasal Türler", ct);
        var topBiyHuc = await GetOrCreateTopicAsync(subBiy.Id, "BIY-HUC", "Hücre", ct);

        return new DemoTaxonomyContainer
        {
            ExamTypeId = examType.Id,
            SubMat = subMat, SubFiz = subFiz, SubTur = subTur, SubKim = subKim, SubBiy = subBiy,
            TopMatTk = topMatTk, TopMatPrb = topMatPrb, TopMatFonk = topMatFonk, TopMatTrig = topMatTrig,
            TopFizElek = topFizElek, TopFizHk = topFizHk,
            TopTurPar = topTurPar, TopTurDbg = topTurDbg,
            TopKimKt = topKimKt, TopBiyHuc = topBiyHuc
        };
    }

    private async Task<Subject> GetOrCreateSubjectAsync(Guid examTypeId, string code, string name, CancellationToken ct)
    {
        var existing = await _db.Set<Subject>().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Code == code || (s.ExamTypeId == examTypeId && s.Name == name), ct);
        if (existing != null) return existing;

        var sub = new Subject { Id = Guid.NewGuid(), ExamTypeId = examTypeId, Code = code, Name = name };
        _db.Set<Subject>().Add(sub);
        await _db.SaveChangesAsync(ct);
        return sub;
    }

    private async Task<Topic> GetOrCreateTopicAsync(Guid subjectId, string code, string name, CancellationToken ct)
    {
        var existing = await _db.Set<Topic>().IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Code == code || (t.SubjectId == subjectId && t.Name == name), ct);
        if (existing != null) return existing;

        var top = new Topic { Id = Guid.NewGuid(), SubjectId = subjectId, Code = code, Name = name };
        _db.Set<Topic>().Add(top);
        await _db.SaveChangesAsync(ct);
        return top;
    }

    private async Task<List<Question>> SeedQuestionsAsync(DemoTaxonomyContainer tax, CancellationToken ct)
    {
        var questions = new List<Question>();

        var topicList = new (Guid SubjectId, Guid TopicId, string Prefix)[]
        {
            (tax.SubTur.Id, tax.TopTurPar.Id, "TUR-PAR"),
            (tax.SubTur.Id, tax.TopTurDbg.Id, "TUR-DBG"),
            (tax.SubMat.Id, tax.TopMatTk.Id, "MAT-TK"),
            (tax.SubMat.Id, tax.TopMatPrb.Id, "MAT-PRB"),
            (tax.SubMat.Id, tax.TopMatFonk.Id, "MAT-FONK"),
            (tax.SubMat.Id, tax.TopMatTrig.Id, "MAT-TRIG"),
            (tax.SubFiz.Id, tax.TopFizElek.Id, "FIZ-ELEK"),
            (tax.SubFiz.Id, tax.TopFizHk.Id, "FIZ-HK"),
            (tax.SubKim.Id, tax.TopKimKt.Id, "KIM-KT"),
            (tax.SubBiy.Id, tax.TopBiyHuc.Id, "BIY-HUC"),
        };

        int qCounter = 0;
        foreach (var t in topicList)
        {
            for (int i = 1; i <= 10; i++)
            {
                qCounter++;
                var qId = Guid.Parse($"d0000000-0000-0000-0001-{qCounter:D12}");
                var existing = await _db.Set<Question>().IgnoreQueryFilters().FirstOrDefaultAsync(q => q.Id == qId, ct);
                if (existing == null)
                {
                    var q = new Question
                    {
                        Id = qId,
                        InstitutionId = DemoInstitutionId,
                        SubjectId = t.SubjectId,
                        TopicId = t.TopicId,
                        Content = $"{t.Prefix} Örnek Soru {i}",
                        Difficulty = DifficultyLevel.Medium,
                        Status = QuestionStatus.Published,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { OptionLabel = "A", Content = "Şık A", IsCorrect = true },
                            new QuestionOption { OptionLabel = "B", Content = "Şık B", IsCorrect = false },
                            new QuestionOption { OptionLabel = "C", Content = "Şık C", IsCorrect = false },
                            new QuestionOption { OptionLabel = "D", Content = "Şık D", IsCorrect = false },
                        }
                    };
                    _db.Set<Question>().Add(q);
                    questions.Add(q);
                }
                else
                {
                    questions.Add(existing);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return questions;
    }

    public enum DemoPerformanceArchetype
    {
        HighAchieving,
        Improving,
        Stable,
        Declining,
        Fluctuating
    }

    private static readonly (string FirstName, string LastName)[] StudentProfiles =
    [
        ("Ahmet", "Yılmaz"),    // 1 (12-A) - Improving
        ("Zeynep", "Demir"),   // 2 (12-A) - Stable
        ("Ece", "Kaya"),       // 3 (12-A) - HighAchieving
        ("Burak", "Yıldız"),   // 4 (12-A) - HighAchieving
        ("Selin", "Aydın"),    // 5 (12-A) - Improving
        ("Emre", "Çetin"),     // 6 (12-A) - Improving
        ("Aylin", "Koç"),      // 7 (12-A) - Stable
        ("Kerem", "Arslan"),   // 8 (12-A) - Stable
        ("Gamze", "Doğan"),    // 9 (12-A) - Declining
        ("Mert", "Şahin"),     // 10 (12-A) - Fluctuating
        ("Can", "Polat"),      // 11 (12-B) - Fluctuating
        ("Zümra", "Yılmaz"),   // 12 (12-B) - HighAchieving
        ("Ozan", "Kılıç"),     // 13 (12-B) - HighAchieving
        ("Buse", "Aksoy"),     // 14 (12-B) - Improving
        ("Tarık", "Erdem"),    // 15 (12-B) - Improving
        ("Melis", "Öztürk"),   // 16 (12-B) - Improving
        ("Kaan", "Bozkurt"),   // 17 (12-B) - Stable
        ("Esra", "Güneş"),     // 18 (12-B) - Stable
        ("Onur", "Korkmaz"),   // 19 (12-B) - Declining
        ("Derya", "Kurt"),     // 20 (12-B) - Declining
        ("Serkan", "Çelik"),   // 21 (12-C) - Fluctuating
        ("Nazlı", "Yavuz"),    // 22 (12-C) - HighAchieving
        ("Umut", "Bulut"),     // 23 (12-C) - HighAchieving
        ("Pınar", "Taş"),      // 24 (12-C) - Improving
        ("Deniz", "Aslan"),    // 25 (12-C) - Improving
        ("Tolga", "Eren"),     // 26 (12-C) - Stable
        ("İrem", "Vural"),     // 27 (12-C) - Stable
        ("Hakan", "Tekin"),    // 28 (12-C) - Declining
        ("Cansu", "Bilgin"),   // 29 (12-C) - Declining
        ("Barış", "Şen")       // 30 (12-C) - Fluctuating
    ];

    private static readonly (string FirstName, string LastName, string Title)[] TeacherProfiles =
    [
        ("Ayşe", "Öğretmen", "Matematik Öğretmeni"),
        ("Mehmet", "Kaya", "Türkçe Öğretmeni"),
        ("Fatma", "Çelik", "Fizik Öğretmeni"),
        ("Ali", "Demir", "Kimya Öğretmeni"),
        ("Zeynep", "Şahin", "Biyoloji Öğretmeni")
    ];

    private static readonly (string FirstName, string LastName)[] CoachProfiles =
    [
        ("Mehmet", "Koç"),
        ("Selin", "Aksoy"),
        ("Emre", "Yurt")
    ];

    private static readonly (string FirstName, string LastName, string Phone, string Relationship)[] ParentProfiles =
    [
        ("Fatma", "Yılmaz", "5550001122", "Mother"),
        ("Emine", "Demir", "5550001123", "Mother"),
        ("Hasan", "Kaya", "5550001124", "Father"),
        ("Ayfer", "Yıldız", "5550001125", "Mother"),
        ("Mustafa", "Aydın", "5550001126", "Father"),
        ("Hatice", "Çetin", "5550001127", "Mother"),
        ("İbrahim", "Koç", "5550001128", "Father"),
        ("Sevim", "Arslan", "5550001129", "Mother"),
        ("Hüseyin", "Doğan", "5550001130", "Father"),
        ("Nurten", "Şahin", "5550001131", "Mother")
    ];

    private static readonly (string Title, DateTimeOffset Date, Guid ExamId)[] DemoExams =
    [
        ("Demo TYT 1", new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000011")),
        ("Demo TYT 2", new DateTimeOffset(2026, 9, 13, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000012")),
        ("Demo TYT 3", new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000013")),
        ("Demo TYT 4", new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000014")),
        ("Demo TYT 5", new DateTimeOffset(2026, 10, 4, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000015")),
        ("Demo TYT 6", new DateTimeOffset(2026, 10, 11, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000016")),
        ("Demo TYT 7", new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000017")),
        ("Demo TYT 8", new DateTimeOffset(2026, 10, 25, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000018")),
        ("Demo TYT 9", new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000019")),
        ("Demo TYT 10", new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-00000000001a")),
    ];

    public static DemoPerformanceArchetype GetArchetype(int studentIndex) => studentIndex switch
    {
        3 or 4 or 12 or 13 or 22 or 23 => DemoPerformanceArchetype.HighAchieving,
        1 or 5 or 6 or 14 or 15 or 16 or 24 or 25 => DemoPerformanceArchetype.Improving,
        2 or 7 or 8 or 17 or 18 or 26 or 27 => DemoPerformanceArchetype.Stable,
        9 or 19 or 20 or 28 or 29 => DemoPerformanceArchetype.Declining,
        10 or 11 or 21 or 30 => DemoPerformanceArchetype.Fluctuating,
        _ => DemoPerformanceArchetype.Stable
    };

    private static (int TurC, int TurW, int TurB, int MatC, int MatW, int MatB, int FizC, int FizW, int FizB, int KimC, int KimW, int KimB)
        CalculateExamScores(int studentIndex, int examIndex, DemoPerformanceArchetype archetype)
    {
        if (studentIndex == 1)
        {
            // Ahmet Yılmaz baseline progression
            return examIndex switch
            {
                1 => (7, 3, 0, 4, 6, 0, 7, 3, 0, 5, 5, 0),
                2 => (7, 3, 0, 4, 6, 0, 6, 4, 0, 6, 4, 0),
                3 => (8, 2, 0, 5, 5, 0, 6, 4, 0, 6, 4, 0),
                4 => (8, 2, 0, 6, 4, 0, 5, 5, 0, 6, 4, 0),
                5 => (8, 2, 0, 7, 3, 0, 4, 6, 0, 6, 4, 0),
                6 => (8, 2, 0, 7, 3, 0, 5, 5, 0, 6, 4, 0),
                7 => (8, 2, 0, 8, 2, 0, 5, 5, 0, 7, 3, 0),
                8 => (9, 1, 0, 8, 2, 0, 5, 5, 0, 7, 3, 0),
                9 => (9, 1, 0, 9, 1, 0, 6, 4, 0, 7, 3, 0),
                10 => (9, 1, 0, 9, 1, 0, 6, 4, 0, 8, 2, 0),
                _ => (8, 2, 0, 7, 3, 0, 5, 5, 0, 6, 4, 0)
            };
        }

        switch (archetype)
        {
            case DemoPerformanceArchetype.HighAchieving:
            {
                int matC = (examIndex >= 7 && studentIndex % 2 == 1) ? 10 : 9;
                int fizC = examIndex <= 4 ? 8 : 9;
                return (9, 1, 0, matC, 10 - matC, 0, fizC, 10 - fizC, 0, 9, 1, 0);
            }
            case DemoPerformanceArchetype.Improving:
            {
                int turC = examIndex <= 3 ? 7 : (examIndex <= 7 ? 8 : 9);
                int matC = examIndex <= 2 ? 4 : (examIndex <= 4 ? 5 : (examIndex <= 6 ? 6 : (examIndex <= 8 ? 7 : 8)));
                int fizC = examIndex <= 3 ? 5 : (examIndex <= 7 ? 6 : 7);
                int kimC = examIndex <= 4 ? 5 : (examIndex <= 7 ? 6 : 7);
                return (turC, 10 - turC, 0, matC, 10 - matC, 0, fizC, 10 - fizC, 0, kimC, 10 - kimC, 0);
            }
            case DemoPerformanceArchetype.Stable:
            {
                int matW = examIndex % 2 == 0 ? 3 : 4;
                int matB = 10 - 6 - matW;
                int fizW = examIndex % 2 == 1 ? 3 : 4;
                int fizB = 10 - 6 - fizW;
                return (8, 2, 0, 6, matW, matB, 6, fizW, fizB, 6, 4, 0);
            }
            case DemoPerformanceArchetype.Declining:
            {
                if (examIndex <= 3) return (8, 2, 0, 8, 2, 0, 8, 2, 0, 7, 3, 0);
                if (examIndex <= 6) return (8, 2, 0, 6, 4, 0, 5, 5, 0, 6, 4, 0);
                return (7, 3, 0, 4, 6, 0, 3, 7, 0, 5, 5, 0);
            }
            case DemoPerformanceArchetype.Fluctuating:
            {
                if (examIndex % 2 == 1) return (9, 1, 0, 8, 2, 0, 8, 2, 0, 7, 3, 0);
                return (6, 4, 0, 4, 6, 0, 4, 6, 0, 5, 5, 0);
            }
            default:
                return (7, 3, 0, 6, 4, 0, 6, 4, 0, 6, 4, 0);
        }
    }

    private async Task<ApplicationUser> EnsureDemoUserAsync(Guid id, string email, string role, string password)
    {
        var existing = await _userManager.FindByIdAsync(id.ToString()) ?? await _userManager.FindByEmailAsync(email);
        if (existing != null) return existing;

        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            InstitutionId = DemoInstitutionId,
            IsActive = true
        };

        var res = await _userManager.CreateAsync(user, password);
        if (res.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
        }
        else
        {
            _logger.LogError("Failed to create expanded demo user {Email}: {Errors}", email, string.Join(", ", res.Errors.Select(e => e.Description)));
        }
        return user;
    }

    private async Task SeedExpandedExamsAndAttemptsAsync(DemoTaxonomyContainer tax, CancellationToken ct)
    {
        // 1. Ensure all 10 Exams exist
        var examEntities = new List<Exam>();
        for (int idx = 0; idx < DemoExams.Length; idx++)
        {
            var eInfo = DemoExams[idx];
            var exam = await _db.Set<Exam>().IgnoreQueryFilters().Include(e => e.Sections).FirstOrDefaultAsync(e => e.Id == eInfo.ExamId, ct);
            if (exam == null)
            {
                exam = new Exam
                {
                    Id = eInfo.ExamId,
                    InstitutionId = DemoInstitutionId,
                    ExamTypeId = tax.ExamTypeId,
                    Code = $"TYT-2026-{(idx + 1):D2}",
                    Title = eInfo.Title,
                    ExamDate = eInfo.Date,
                    Status = ExamStatus.Published,
                    WrongAnswerPenalty = 4m
                };

                var secTur = new ExamSection { Id = Guid.Parse($"d0000000-0000-0040-{(idx + 1):D4}-000000000001"), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubTur.Id, DisplayOrder = 1, QuestionCount = 10 };
                var secMat = new ExamSection { Id = Guid.Parse($"d0000000-0000-0040-{(idx + 1):D4}-000000000002"), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubMat.Id, DisplayOrder = 2, QuestionCount = 10 };
                var secFiz = new ExamSection { Id = Guid.Parse($"d0000000-0000-0040-{(idx + 1):D4}-000000000003"), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubFiz.Id, DisplayOrder = 3, QuestionCount = 10 };
                var secKim = new ExamSection { Id = Guid.Parse($"d0000000-0000-0040-{(idx + 1):D4}-000000000004"), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubKim.Id, DisplayOrder = 4, QuestionCount = 10 };
                var secBiy = new ExamSection { Id = Guid.Parse($"d0000000-0000-0040-{(idx + 1):D4}-000000000005"), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubBiy.Id, DisplayOrder = 5, QuestionCount = 0 };

                exam.Sections.AddRange([secTur, secMat, secFiz, secKim, secBiy]);
                _db.Set<Exam>().Add(exam);
                await _db.SaveChangesAsync(ct);
            }
            examEntities.Add(exam);
        }

        // 2. Ensure Attempts for all 30 Students across all 10 Exams (300 attempts total)
        for (int s = 1; s <= 30; s++)
        {
            var studentId = DemoStudentEntityGuid(s);
            var archetype = GetArchetype(s);

            for (int eIdx = 1; eIdx <= 10; eIdx++)
            {
                var exam = examEntities[eIdx - 1];
                var attemptId = DemoAttemptGuid(s, eIdx);

                var existingAttempt = await _db.Set<StudentExamAttempt>().IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == attemptId, ct);
                if (existingAttempt != null)
                {
                    continue;
                }

                var (turC, turW, turB, matC, matW, matB, fizC, fizW, fizB, kimC, kimW, kimB) = CalculateExamScores(s, eIdx, archetype);

                int totalCorrect = turC + matC + fizC + kimC;
                int totalWrong = turW + matW + fizW + kimW;
                int totalBlank = turB + matB + fizB + kimB;
                decimal totalNet = totalCorrect - (totalWrong / 4m);

                var attempt = new StudentExamAttempt
                {
                    Id = attemptId,
                    InstitutionId = DemoInstitutionId,
                    ExamId = exam.Id,
                    StudentId = studentId,
                    AttemptNumber = 1,
                    TakenAt = exam.ExamDate,
                    Status = AttemptStatus.Finalized,
                    FinalizedAt = exam.ExamDate.AddHours(3),
                    IsRawAvailable = true,
                    TotalCorrect = totalCorrect,
                    TotalWrong = totalWrong,
                    TotalBlank = totalBlank,
                    TotalNet = totalNet
                };

                // Section order: 1: Tur, 2: Mat, 3: Fiz, 4: Kim
                var secTur = exam.Sections.First(sec => sec.DisplayOrder == 1);
                var secMat = exam.Sections.First(sec => sec.DisplayOrder == 2);
                var secFiz = exam.Sections.First(sec => sec.DisplayOrder == 3);
                var secKim = exam.Sections.First(sec => sec.DisplayOrder == 4);

                attempt.SubjectResults = new List<StudentExamSubjectResult>
                {
                    new StudentExamSubjectResult
                    {
                        Id = DemoSubjectResultGuid(s, eIdx, 1),
                        InstitutionId = DemoInstitutionId,
                        StudentExamAttemptId = attempt.Id,
                        ExamSectionId = secTur.Id,
                        Correct = turC,
                        Wrong = turW,
                        Blank = turB,
                        Net = turC - (turW / 4m)
                    },
                    new StudentExamSubjectResult
                    {
                        Id = DemoSubjectResultGuid(s, eIdx, 2),
                        InstitutionId = DemoInstitutionId,
                        StudentExamAttemptId = attempt.Id,
                        ExamSectionId = secMat.Id,
                        Correct = matC,
                        Wrong = matW,
                        Blank = matB,
                        Net = matC - (matW / 4m)
                    },
                    new StudentExamSubjectResult
                    {
                        Id = DemoSubjectResultGuid(s, eIdx, 3),
                        InstitutionId = DemoInstitutionId,
                        StudentExamAttemptId = attempt.Id,
                        ExamSectionId = secFiz.Id,
                        Correct = fizC,
                        Wrong = fizW,
                        Blank = fizB,
                        Net = fizC - (fizW / 4m)
                    },
                    new StudentExamSubjectResult
                    {
                        Id = DemoSubjectResultGuid(s, eIdx, 4),
                        InstitutionId = DemoInstitutionId,
                        StudentExamAttemptId = attempt.Id,
                        ExamSectionId = secKim.Id,
                        Correct = kimC,
                        Wrong = kimW,
                        Blank = kimB,
                        Net = kimC - (kimW / 4m)
                    }
                };

                // 8 Active Topics
                // 1. Türkçe Topics (Paragraf 5, DilBilgisi 5)
                int parC = Math.Min(5, (turC + 1) / 2);
                int dbgC = turC - parC;
                int remTurW = turW;
                int parW = Math.Min(remTurW, 5 - parC);
                int dbgW = remTurW - parW;
                int parB = 5 - parC - parW;
                int dbgB = 5 - dbgC - dbgW;

                // 2. Matematik Topics (TK 3, Prb 3, Fonk 2, Trig 2)
                int tkC = Math.Min(3, (matC + 2) / 3);
                int prbC = Math.Min(3, (matC + 1) / 3);
                int fonkC = Math.Min(2, Math.Max(0, matC - tkC - prbC));
                int trigC = matC - tkC - prbC - fonkC;
                int remMatW = matW;
                int tkCap = 3 - tkC; int tkW = Math.Min(remMatW, tkCap); remMatW -= tkW; int tkB = tkCap - tkW;
                int prbCap = 3 - prbC; int prbW = Math.Min(remMatW, prbCap); remMatW -= prbW; int prbB = prbCap - prbW;
                int fonkCap = 2 - fonkC; int fonkW = Math.Min(remMatW, fonkCap); remMatW -= fonkW; int fonkB = fonkCap - fonkW;
                int trigCap = 2 - trigC; int trigW = Math.Min(remMatW, trigCap); remMatW -= trigW; int trigB = trigCap - trigW;

                // 3. Fizik Topics (Elektrik 5, HK 5)
                int elekC = (archetype == DemoPerformanceArchetype.Declining && eIdx >= 7) ? 1 : Math.Min(5, (fizC + 1) / 2);
                int hkC = fizC - elekC;
                int remFizW = fizW;
                int elekCap = 5 - elekC; int elekW = Math.Min(remFizW, elekCap); remFizW -= elekW; int elekB = elekCap - elekW;
                int hkCap = 5 - hkC; int hkW = Math.Min(remFizW, hkCap); remFizW -= hkW; int hkB = hkCap - hkW;

                attempt.TopicResults = new List<StudentExamTopicResult>
                {
                    new StudentExamTopicResult { Id = DemoTopicResultGuid(s, eIdx, 1), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = secTur.Id, TopicId = tax.TopTurPar.Id, Correct = parC, Wrong = parW, Blank = parB, Net = parC - (parW / 4m) },
                    new StudentExamTopicResult { Id = DemoTopicResultGuid(s, eIdx, 2), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = secTur.Id, TopicId = tax.TopTurDbg.Id, Correct = dbgC, Wrong = dbgW, Blank = dbgB, Net = dbgC - (dbgW / 4m) },
                    new StudentExamTopicResult { Id = DemoTopicResultGuid(s, eIdx, 3), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = secMat.Id, TopicId = tax.TopMatTk.Id, Correct = tkC, Wrong = tkW, Blank = tkB, Net = tkC - (tkW / 4m) },
                    new StudentExamTopicResult { Id = DemoTopicResultGuid(s, eIdx, 4), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = secMat.Id, TopicId = tax.TopMatPrb.Id, Correct = prbC, Wrong = prbW, Blank = prbB, Net = prbC - (prbW / 4m) },
                    new StudentExamTopicResult { Id = DemoTopicResultGuid(s, eIdx, 5), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = secMat.Id, TopicId = tax.TopMatFonk.Id, Correct = fonkC, Wrong = fonkW, Blank = fonkB, Net = fonkC - (fonkW / 4m) },
                    new StudentExamTopicResult { Id = DemoTopicResultGuid(s, eIdx, 6), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = secMat.Id, TopicId = tax.TopMatTrig.Id, Correct = trigC, Wrong = trigW, Blank = trigB, Net = trigC - (trigW / 4m) },
                    new StudentExamTopicResult { Id = DemoTopicResultGuid(s, eIdx, 7), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = secFiz.Id, TopicId = tax.TopFizElek.Id, Correct = elekC, Wrong = elekW, Blank = elekB, Net = elekC - (elekW / 4m) },
                    new StudentExamTopicResult { Id = DemoTopicResultGuid(s, eIdx, 8), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = secFiz.Id, TopicId = tax.TopFizHk.Id, Correct = hkC, Wrong = hkW, Blank = hkB, Net = hkC - (hkW / 4m) },
                };

                _db.Set<StudentExamAttempt>().Add(attempt);
            }

            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task SeedCoachTasksAsync(CancellationToken ct)
    {
        var coach1User = DemoCoachUserGuid(1);
        var coach2User = DemoCoachUserGuid(2);
        var coach3User = DemoCoachUserGuid(3);

        var s1 = DemoStudentEntityGuid(1);
        var s9 = DemoStudentEntityGuid(9);
        var s10 = DemoStudentEntityGuid(10);
        var s11 = DemoStudentEntityGuid(11);
        var s21 = DemoStudentEntityGuid(21);

        var tasks = new[]
        {
            new CoachTask
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-000000000040"),
                InstitutionId = DemoInstitutionId,
                StudentId = s1,
                CoachUserId = coach1User,
                Title = "Paragraf Tekrar Çalışması",
                Description = "Son deneme sonucuna göre Türkçe Paragraf soruları üzerinde 30 soru çözümü.",
                DueDate = DateTimeOffset.UtcNow.AddDays(-1),
                DurationMinutes = 45,
                SubjectName = "Türkçe",
                TopicName = "Paragraf",
                Status = CoachTaskStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new CoachTask
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-000000000041"),
                InstitutionId = DemoInstitutionId,
                StudentId = s1,
                CoachUserId = coach1User,
                Title = "Trigonometri Formül ve Soru Uygulaması",
                Description = "Yarım açı ve toplam-fark formüllerini içeren 40 soru çözümü ve analiz.",
                DueDate = DateTimeOffset.UtcNow.AddDays(2),
                DurationMinutes = 60,
                SubjectName = "Matematik",
                TopicName = "Trigonometri",
                Status = CoachTaskStatus.Active
            },
            new CoachTask
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-000000000042"),
                InstitutionId = DemoInstitutionId,
                StudentId = s9,
                CoachUserId = coach1User,
                Title = "Elektrik Devreleri Pekiştirme",
                Description = "Eşdeğer direnç ve Ohm kanunu odaklı 25 temel soru çözümü.",
                DueDate = DateTimeOffset.UtcNow.AddDays(1),
                DurationMinutes = 45,
                SubjectName = "Fizik",
                TopicName = "Elektrik",
                Status = CoachTaskStatus.Active
            },
            new CoachTask
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-000000000043"),
                InstitutionId = DemoInstitutionId,
                StudentId = s9,
                CoachUserId = coach1User,
                Title = "Hücre ve Kalıtım Soru Çözümü",
                Description = "Mitoz-mayoz bölünmeler ve temel kalıtım çaprazlama tekrarları.",
                DueDate = DateTimeOffset.UtcNow.AddDays(-3),
                DurationMinutes = 45,
                SubjectName = "Biyoloji",
                TopicName = "Hücre",
                Status = CoachTaskStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow.AddDays(-4)
            },
            new CoachTask
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-000000000044"),
                InstitutionId = DemoInstitutionId,
                StudentId = s10,
                CoachUserId = coach1User,
                Title = "Problemler Rutin Pratiği",
                Description = "Dalgalanan netleri stabilize etmek için günlük 20 karma problem pratiği.",
                DueDate = DateTimeOffset.UtcNow.AddDays(3),
                DurationMinutes = 50,
                SubjectName = "Matematik",
                TopicName = "Problemler",
                Status = CoachTaskStatus.Active
            },
            new CoachTask
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-000000000045"),
                InstitutionId = DemoInstitutionId,
                StudentId = s11,
                CoachUserId = coach2User,
                Title = "Fonksiyonlar Grafikler ve Analiz",
                Description = "Fonksiyon grafiği okuma ve bileşke fonksiyon soru çözümü.",
                DueDate = DateTimeOffset.UtcNow.AddDays(2),
                DurationMinutes = 60,
                SubjectName = "Matematik",
                TopicName = "Fonksiyonlar",
                Status = CoachTaskStatus.Active
            },
            new CoachTask
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-000000000046"),
                InstitutionId = DemoInstitutionId,
                StudentId = s21,
                CoachUserId = coach3User,
                Title = "Temel Kavramlar Pekiştirme",
                Description = "Sözel branş için TYT temel matematik soru çözüm etüdü.",
                DueDate = DateTimeOffset.UtcNow.AddDays(4),
                DurationMinutes = 45,
                SubjectName = "Matematik",
                TopicName = "Temel Kavramlar",
                Status = CoachTaskStatus.Active
            }
        };

        foreach (var t in tasks)
        {
            if (!await _db.Set<CoachTask>().IgnoreQueryFilters().AnyAsync(x => x.Id == t.Id, ct))
            {
                _db.Set<CoachTask>().Add(t);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedNotificationsAsync(CancellationToken ct)
    {
        var n1Id = Guid.Parse("d0000000-0000-0000-0000-000000000051");
        if (!await _db.Set<Notification>().IgnoreQueryFilters().AnyAsync(n => n.Id == n1Id, ct))
        {
            _db.Set<Notification>().Add(new Notification
            {
                Id = n1Id,
                InstitutionId = DemoInstitutionId,
                UserId = DemoStudentUserId,
                Type = NotificationType.ExamResultPublished,
                Title = "Demo TYT 5 Sınav Sonucu",
                Message = "Demo TYT 5 sınav sonucunuz ve detaylı analiziniz yayımlandı.",
                IsRead = true,
                ReadAt = DateTimeOffset.UtcNow.AddDays(-1)
            });
        }

        var n2Id = Guid.Parse("d0000000-0000-0000-0000-000000000052");
        if (!await _db.Set<Notification>().IgnoreQueryFilters().AnyAsync(n => n.Id == n2Id, ct))
        {
            _db.Set<Notification>().Add(new Notification
            {
                Id = n2Id,
                InstitutionId = DemoInstitutionId,
                UserId = DemoStudentUserId,
                Type = NotificationType.CoachTaskAssigned,
                Title = "Koç Görevi Tamamlandı",
                Message = "Paragraf Tekrar Çalışması görevi tamamlandı.",
                IsRead = false
            });
        }

        var n3Id = Guid.Parse("d0000000-0000-0000-0000-000000000053");
        if (!await _db.Set<Notification>().IgnoreQueryFilters().AnyAsync(n => n.Id == n3Id, ct))
        {
            _db.Set<Notification>().Add(new Notification
            {
                Id = n3Id,
                InstitutionId = DemoInstitutionId,
                UserId = DemoCoachUserId,
                Type = NotificationType.CoachTaskAssigned,
                Title = "Yeni Öğrenci Ataması",
                Message = "Ahmet Yılmaz portföyünüze atandı.",
                IsRead = true,
                ReadAt = DateTimeOffset.UtcNow.AddDays(-5)
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    // ───────────────────────── Showcase enrichment (idempotent) ─────────────────────────
    // Adds linked classrooms, teachers, coaches, parents, students, question-level/style-tagged exam data,
    // an active study plan and sample notes. Every record is scoped to DemoInstitutionId
    // and uses deterministic IDs so repeated runs never duplicate data.

    private async Task SeedShowcaseEnrichmentAsync(CancellationToken ct)
    {
        var tax = await SeedTaxonomyAsync(ct);
        await SeedExpandedHierarchyAsync(tax, ct);
        await SeedExpandedExamsAndAttemptsAsync(tax, ct);
        await SeedQuestionStyleAssignmentsAsync(ct);
        await SeedQuestionLevelResultsAsync(ct);
        await SeedCoachTasksAsync(ct);
        await SeedStudyPlanAsync(ct);
        await SeedNotesAsync(ct);
        await SeedMeetingsAsync(ct);
        await SeedConversationsAndMessagesAsync(ct);
        await SeedMaterialsAsync(ct);
    }

    private async Task SeedExpandedHierarchyAsync(DemoTaxonomyContainer tax, CancellationToken ct)
    {
        // 1. Ensure Classrooms (12-A, 12-B, 12-C)
        var classrooms = new[]
        {
            (Id: DemoClassroom12AId, Name: "12-A", Branch: "Sayısal"),
            (Id: DemoClassroom12BId, Name: "12-B", Branch: "Eşit Ağırlık"),
            (Id: DemoClassroom12CId, Name: "12-C", Branch: "Sözel")
        };
        foreach (var c in classrooms)
        {
            if (!await _db.Set<Classroom>().IgnoreQueryFilters().AnyAsync(x => x.Id == c.Id, ct))
            {
                _db.Set<Classroom>().Add(new Classroom
                {
                    Id = c.Id,
                    InstitutionId = DemoInstitutionId,
                    Name = c.Name,
                    Branch = c.Branch,
                    GradeLevel = 12,
                    AcademicYear = "2026-2027"
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        // 2. Ensure Teachers (5 teachers)
        for (int t = 1; t <= 5; t++)
        {
            var profile = TeacherProfiles[t - 1];
            var userId = DemoTeacherUserGuid(t);
            var entityId = DemoTeacherEntityGuid(t);
            var email = t == 1 ? _settings.TeacherEmail : $"ogretmen{t}@bilimakademi.com";

            await EnsureDemoUserAsync(userId, email, Roles.Teacher, _settings.TeacherPassword!);

            if (!await _db.Set<Teacher>().IgnoreQueryFilters().AnyAsync(x => x.Id == entityId, ct))
            {
                _db.Set<Teacher>().Add(new Teacher
                {
                    Id = entityId,
                    InstitutionId = DemoInstitutionId,
                    UserId = userId,
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    Title = profile.Title
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        // 3. Ensure ClassroomTeacher links
        // Ayşe (T1) -> 12-A only (Primary). Keep 12-B unassigned so UAT assignment test passes!
        // Mehmet (T2) -> 12-A
        // Fatma (T3) -> 12-B (Primary)
        // Ali (T4) -> 12-B
        // Zeynep (T5) -> 12-C (Primary)
        var classroomTeacherLinks = new (Guid LinkId, int TeacherIdx, Guid ClassroomId, bool IsPrimary)[]
        {
            (Guid.Parse("d0000000-0000-0000-0000-000000000062"), 1, DemoClassroom12AId, true),
            (Guid.Parse("d0000000-0000-0033-0000-000000000002"), 2, DemoClassroom12AId, false),
            (Guid.Parse("d0000000-0000-0033-0000-000000000003"), 3, DemoClassroom12BId, true),
            (Guid.Parse("d0000000-0000-0033-0000-000000000004"), 4, DemoClassroom12BId, false),
            (Guid.Parse("d0000000-0000-0033-0000-000000000005"), 5, DemoClassroom12CId, true)
        };
        foreach (var l in classroomTeacherLinks)
        {
            if (!await _db.Set<ClassroomTeacher>().IgnoreQueryFilters().AnyAsync(x => x.Id == l.LinkId, ct))
            {
                _db.Set<ClassroomTeacher>().Add(new ClassroomTeacher
                {
                    Id = l.LinkId,
                    InstitutionId = DemoInstitutionId,
                    ClassroomId = l.ClassroomId,
                    TeacherId = DemoTeacherEntityGuid(l.TeacherIdx),
                    IsPrimary = l.IsPrimary
                });
            }
        }

        // 4. Ensure TeacherSubject links
        var teacherSubjectLinks = new (Guid LinkId, int TeacherIdx, Guid SubjectId)[]
        {
            (Guid.Parse("d0000000-0000-0000-0000-000000000066"), 1, tax.SubMat.Id),
            (Guid.Parse("d0000000-0000-0034-0000-000000000002"), 2, tax.SubTur.Id),
            (Guid.Parse("d0000000-0000-0034-0000-000000000003"), 3, tax.SubFiz.Id),
            (Guid.Parse("d0000000-0000-0034-0000-000000000004"), 4, tax.SubKim.Id),
            (Guid.Parse("d0000000-0000-0034-0000-000000000005"), 5, tax.SubBiy.Id)
        };
        foreach (var ts in teacherSubjectLinks)
        {
            if (!await _db.Set<TeacherSubject>().IgnoreQueryFilters().AnyAsync(x => x.Id == ts.LinkId, ct))
            {
                _db.Set<TeacherSubject>().Add(new TeacherSubject
                {
                    Id = ts.LinkId,
                    InstitutionId = DemoInstitutionId,
                    TeacherId = DemoTeacherEntityGuid(ts.TeacherIdx),
                    SubjectId = ts.SubjectId
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        // 5. Ensure Coaches (3 coaches)
        for (int c = 1; c <= 3; c++)
        {
            var profile = CoachProfiles[c - 1];
            var userId = DemoCoachUserGuid(c);
            var entityId = DemoCoachEntityGuid(c);
            var email = c == 1 ? _settings.CoachEmail : $"koc{c}@bilimakademi.com";

            await EnsureDemoUserAsync(userId, email, Roles.Coach, _settings.CoachPassword!);

            if (!await _db.Set<Coach>().IgnoreQueryFilters().AnyAsync(x => x.Id == entityId, ct))
            {
                _db.Set<Coach>().Add(new Coach
                {
                    Id = entityId,
                    InstitutionId = DemoInstitutionId,
                    UserId = userId,
                    FirstName = profile.FirstName,
                    LastName = profile.LastName
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        // 6. Ensure Parents (10 parents)
        for (int p = 1; p <= 10; p++)
        {
            var profile = ParentProfiles[p - 1];
            var userId = DemoParentUserGuid(p);
            var entityId = DemoParentEntityGuid(p);
            var email = p == 1 ? _settings.ParentEmail : $"veli{p}@bilimakademi.com";

            await EnsureDemoUserAsync(userId, email, Roles.Parent, _settings.ParentPassword!);

            if (!await _db.Set<Parent>().IgnoreQueryFilters().AnyAsync(x => x.Id == entityId, ct))
            {
                _db.Set<Parent>().Add(new Parent
                {
                    Id = entityId,
                    InstitutionId = DemoInstitutionId,
                    UserId = userId,
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    Email = email,
                    Phone = profile.Phone
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        // 7. Ensure Students (30 students)
        for (int s = 1; s <= 30; s++)
        {
            var profile = StudentProfiles[s - 1];
            var userId = DemoStudentUserGuid(s);
            var entityId = DemoStudentEntityGuid(s);
            var email = s == 1 ? _settings.StudentEmail : $"ogrenci{s}@bilimakademi.com";
            var studentNumber = $"12{s:D2}";

            await EnsureDemoUserAsync(userId, email, Roles.Student, _settings.StudentPassword!);

            if (!await _db.Set<Student>().IgnoreQueryFilters().AnyAsync(x => x.Id == entityId, ct))
            {
                _db.Set<Student>().Add(new Student
                {
                    Id = entityId,
                    InstitutionId = DemoInstitutionId,
                    UserId = userId,
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    StudentNumber = studentNumber,
                    GradeLevel = 12,
                    SchoolName = "Demo Anadolu Lisesi",
                    EnrollmentDate = DateTimeOffset.UtcNow.AddMonths(-6),
                    Status = StudentStatus.Active
                });
            }
        }
        await _db.SaveChangesAsync(ct);

        // 8. Ensure ClassroomStudent links (10 per classroom)
        for (int s = 1; s <= 30; s++)
        {
            var linkId = s == 1 ? Guid.Parse("d0000000-0000-0000-0000-000000000063")
                       : s == 2 ? Guid.Parse("d0000000-0000-0000-0000-000000000064")
                       : Guid.Parse($"d0000000-0000-0030-0000-{s:D12}");
            var classroomId = s <= 10 ? DemoClassroom12AId : s <= 20 ? DemoClassroom12BId : DemoClassroom12CId;
            var studentId = DemoStudentEntityGuid(s);

            if (!await _db.Set<ClassroomStudent>().IgnoreQueryFilters().AnyAsync(x => x.Id == linkId, ct))
            {
                _db.Set<ClassroomStudent>().Add(new ClassroomStudent
                {
                    Id = linkId,
                    InstitutionId = DemoInstitutionId,
                    ClassroomId = classroomId,
                    StudentId = studentId
                });
            }
        }

        // 9. Ensure StudentCoachAssignment links (10 per coach)
        for (int s = 1; s <= 30; s++)
        {
            var linkId = s == 1 ? Guid.Parse("d0000000-0000-0000-0000-00000000000c")
                       : s == 2 ? Guid.Parse("d0000000-0000-0000-0000-000000000065")
                       : Guid.Parse($"d0000000-0000-0031-0000-{s:D12}");
            var coachId = s <= 10 ? DemoCoachEntityGuid(1) : s <= 20 ? DemoCoachEntityGuid(2) : DemoCoachEntityGuid(3);
            var studentId = DemoStudentEntityGuid(s);

            if (!await _db.Set<StudentCoachAssignment>().IgnoreQueryFilters().AnyAsync(x => x.Id == linkId, ct))
            {
                _db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
                {
                    Id = linkId,
                    InstitutionId = DemoInstitutionId,
                    StudentId = studentId,
                    CoachId = coachId,
                    IsPrimary = true,
                    IsActive = true,
                    AssignedAt = DateTimeOffset.UtcNow.AddMonths(-3)
                });
            }
        }

        // 10. Ensure StudentParent links (P1 -> S1 only, P2 -> S2, ..., P10 -> S10)
        for (int p = 1; p <= 10; p++)
        {
            var linkId = p == 1 ? Guid.Parse("d0000000-0000-0000-0000-00000000000b")
                                : Guid.Parse($"d0000000-0000-0032-0000-{p:D12}");
            var parentId = DemoParentEntityGuid(p);
            var studentId = DemoStudentEntityGuid(p);
            var relationship = ParentProfiles[p - 1].Relationship;

            if (!await _db.Set<StudentParent>().IgnoreQueryFilters().AnyAsync(x => x.Id == linkId, ct))
            {
                _db.Set<StudentParent>().Add(new StudentParent
                {
                    Id = linkId,
                    InstitutionId = DemoInstitutionId,
                    StudentId = studentId,
                    ParentId = parentId,
                    RelationshipType = relationship,
                    IsActive = true
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static Guid DemoQuestionId(int counter) => Guid.Parse($"d0000000-0000-0000-0001-{counter:D12}");

    // Topic block index (0..9) follows SeedQuestionsAsync ordering: 10 questions per topic.
    private static readonly string[][] TopicStyleCodes =
    [
        [QuestionStyleDefaults.Codes.Interpretation, QuestionStyleDefaults.Codes.NewGeneration], // TUR-PAR
        [QuestionStyleDefaults.Codes.Conceptual],                                                 // TUR-DBG
        [QuestionStyleDefaults.Codes.Calculation],                                                // MAT-TK
        [QuestionStyleDefaults.Codes.Problem, QuestionStyleDefaults.Codes.NewGeneration],         // MAT-PRB
        [QuestionStyleDefaults.Codes.GraphTable, QuestionStyleDefaults.Codes.Calculation],        // MAT-FONK
        [QuestionStyleDefaults.Codes.Calculation, QuestionStyleDefaults.Codes.MultiStep],         // MAT-TRIG
        [QuestionStyleDefaults.Codes.Conceptual, QuestionStyleDefaults.Codes.GraphTable],         // FIZ-ELEK
        [QuestionStyleDefaults.Codes.Problem, QuestionStyleDefaults.Codes.MultiStep],             // FIZ-HK
        [QuestionStyleDefaults.Codes.Conceptual],                                                 // KIM-KT
        [QuestionStyleDefaults.Codes.Visual],                                                     // BIY-HUC
    ];

    private async Task SeedQuestionStyleAssignmentsAsync(CancellationToken ct)
    {
        await QuestionStyleDefaults.SeedInitialStylesAsync(_db, ct);

        var styleIds = await _db.Set<QuestionStyle>().AsNoTracking()
            .ToDictionaryAsync(s => s.Code, s => s.Id, StringComparer.OrdinalIgnoreCase, ct);

        var questionIds = Enumerable.Range(1, 100).Select(DemoQuestionId).ToList();
        var existingQuestionIds = await _db.Set<Question>().IgnoreQueryFilters()
            .Where(q => questionIds.Contains(q.Id))
            .Select(q => q.Id)
            .ToListAsync(ct);
        var existingPairs = await _db.Set<QuestionStyleAssignment>().AsNoTracking()
            .Where(a => questionIds.Contains(a.QuestionId))
            .Select(a => new { a.QuestionId, a.QuestionStyleId })
            .ToListAsync(ct);
        var pairSet = existingPairs.Select(p => (p.QuestionId, p.QuestionStyleId)).ToHashSet();
        var existingSet = existingQuestionIds.ToHashSet();

        for (int counter = 1; counter <= 100; counter++)
        {
            var qId = DemoQuestionId(counter);
            if (!existingSet.Contains(qId)) continue;

            foreach (var code in TopicStyleCodes[(counter - 1) / 10])
            {
                if (!styleIds.TryGetValue(code, out var styleId)) continue;
                if (pairSet.Add((qId, styleId)))
                {
                    _db.Set<QuestionStyleAssignment>().Add(new QuestionStyleAssignment { QuestionId = qId, QuestionStyleId = styleId });
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Assembles 40 questions into demo exams 1-4 and records raw per-question answers that
    /// are exactly consistent with the already-seeded per-subject correct/wrong totals.
    /// Demo TYT 5 is intentionally left aggregate-only (used by manual/Excel result entry demos).
    /// </summary>
    private async Task SeedQuestionLevelResultsAsync(CancellationToken ct)
    {
        // Per section (by DisplayOrder): question counters in "most likely correct first" order.
        var sectionPlans = new Dictionary<int, int[]>
        {
            [1] = [11, 12, 13, 14, 15, 1, 2, 3, 4, 5],          // Türkçe: Dil Bilgisi then Paragraf
            [2] = [21, 22, 23, 41, 42, 31, 32, 33, 51, 52],     // Matematik: TK, Fonksiyon, Problem, Trigonometri
            [3] = [71, 72, 73, 74, 75, 61, 62, 63, 64, 65],     // Fizik: Hareket-Kuvvet then Elektrik
            [4] = [81, 82, 83, 84, 85, 86, 87, 88, 89, 90],     // Kimya
        };

        for (int examIdx = 1; examIdx <= 4; examIdx++)
        {
            var examId = Guid.Parse($"d0000000-0000-0000-0000-0000000000{10 + examIdx}");
            var attemptId = Guid.Parse($"d0000000-0000-0000-0000-0000000000{20 + examIdx}");

            var exam = await _db.Set<Exam>().IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == examId, ct);
            var attempt = await _db.Set<StudentExamAttempt>().IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(a => a.Id == attemptId, ct);
            if (exam == null || attempt == null) continue;

            var sections = await _db.Set<ExamSection>().IgnoreQueryFilters().AsNoTracking()
                .Where(s => s.ExamId == examId && !s.IsDeleted)
                .ToListAsync(ct);
            var subjectResults = await _db.Set<StudentExamSubjectResult>().IgnoreQueryFilters().AsNoTracking()
                .Where(r => r.StudentExamAttemptId == attemptId && !r.IsDeleted)
                .ToListAsync(ct);

            foreach (var (displayOrder, counters) in sectionPlans)
            {
                var section = sections.FirstOrDefault(s => s.DisplayOrder == displayOrder);
                if (section == null) continue;
                var subjectResult = subjectResults.FirstOrDefault(r => r.ExamSectionId == section.Id);
                if (subjectResult == null) continue;

                // Display numbering follows taxonomy order; correctness follows the ranked plan.
                var numbered = counters.OrderBy(c => c).ToList();
                var correctSet = counters.Take(subjectResult.Correct).ToHashSet();
                var wrongSet = counters.Skip(subjectResult.Correct).Take(subjectResult.Wrong).ToHashSet();

                for (int i = 0; i < numbered.Count; i++)
                {
                    var counter = numbered[i];
                    var qId = DemoQuestionId(counter);
                    var eqId = Guid.Parse($"d0000000-0000-0002-{examIdx:D4}-{counter:D12}");
                    var rId = Guid.Parse($"d0000000-0000-0003-{examIdx:D4}-{counter:D12}");

                    if (!await _db.Set<ExamQuestion>().IgnoreQueryFilters().AnyAsync(x => x.Id == eqId, ct))
                    {
                        _db.Set<ExamQuestion>().Add(new ExamQuestion
                        {
                            Id = eqId,
                            InstitutionId = DemoInstitutionId,
                            ExamId = examId,
                            ExamSectionId = section.Id,
                            QuestionId = qId,
                            QuestionNumber = i + 1,
                            Order = i + 1,
                            CorrectOption = "A"
                        });
                    }

                    if (!await _db.Set<StudentExamQuestionResult>().IgnoreQueryFilters().AnyAsync(x => x.Id == rId, ct))
                    {
                        bool isCorrect = correctSet.Contains(counter);
                        bool isWrong = wrongSet.Contains(counter);
                        _db.Set<StudentExamQuestionResult>().Add(new StudentExamQuestionResult
                        {
                            Id = rId,
                            InstitutionId = DemoInstitutionId,
                            StudentExamAttemptId = attemptId,
                            ExamQuestionId = eqId,
                            QuestionId = qId,
                            SelectedOption = isCorrect ? "A" : isWrong ? "C" : null,
                            IsCorrect = isCorrect ? true : isWrong ? false : null,
                            IsBlank = !isCorrect && !isWrong,
                            NetContribution = isCorrect ? 1m : isWrong ? -1m / exam.WrongAnswerPenalty : 0m
                        });
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task SeedStudyPlanAsync(CancellationToken ct)
    {
        var topicIds = new[]
        {
            DemoQuestionId(1), DemoQuestionId(11), DemoQuestionId(31), DemoQuestionId(41),
            DemoQuestionId(51), DemoQuestionId(61), DemoQuestionId(71), DemoQuestionId(81), DemoQuestionId(91)
        };
        var topicInfo = await _db.Set<Question>().IgnoreQueryFilters().AsNoTracking()
            .Where(q => topicIds.Contains(q.Id))
            .Select(q => new { q.Id, q.SubjectId, q.TopicId })
            .ToListAsync(ct);
        var allTopicIds = topicInfo.Select(t => t.TopicId).Distinct().ToList();
        var topics = await _db.Set<Topic>().IgnoreQueryFilters().AsNoTracking()
            .Where(t => allTopicIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);
        var subjectIds = topicInfo.Select(t => t.SubjectId).Distinct().ToList();
        var subjects = await _db.Set<Subject>().IgnoreQueryFilters().AsNoTracking()
            .Where(s => subjectIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);

        var start = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero).AddDays(-2);

        // 1. Primary Student 1 (Ahmet Yılmaz - Improving) Study Plan
        if (!await _db.Set<StudyPlan>().IgnoreQueryFilters().AnyAsync(p => p.Id == DemoStudyPlanId, ct) &&
            !await _db.Set<StudyPlan>().AnyAsync(p => p.StudentId == DemoStudentId && p.Status == StudyPlanStatus.Active, ct))
        {
            var specs = new (int QIdx, AcademicClassificationState State, int Priority, AcademicRecommendationType Type, bool Done)[]
            {
                (51, AcademicClassificationState.Critical, 92, AcademicRecommendationType.RemediateCritical, true),
                (31, AcademicClassificationState.Weak, 78, AcademicRecommendationType.StrengthenWeak, true),
                (61, AcademicClassificationState.Weak, 74, AcademicRecommendationType.MonitorDeclining, false),
                (1, AcademicClassificationState.Developing, 55, AcademicRecommendationType.ReinforceDeveloping, false),
                (41, AcademicClassificationState.Developing, 50, AcademicRecommendationType.ReinforceDeveloping, false),
            };

            var plan = new StudyPlan
            {
                Id = DemoStudyPlanId,
                InstitutionId = DemoInstitutionId,
                StudentId = DemoStudentId,
                Title = "Haftalık Çalışma Planı",
                StartDate = start,
                EndDate = start.AddDays(7),
                Status = StudyPlanStatus.Active,
                Source = StudyPlanSource.AcademicIntelligence,
                RecommendationPolicyVersion = "demo-v1",
                StudyPlanPolicyVersion = "demo-v1",
                ActivatedAt = start,
                PlanDays = 7,
                DailyStudyMinutes = 60,
                SessionMinutes = 45,
                TotalAvailableMinutes = 420,
                ScheduledRecommendationCount = specs.Length,
                UnscheduledRecommendationCount = 0
            };

            int day = 0;
            foreach (var s in specs)
            {
                var info = topicInfo.FirstOrDefault(t => t.Id == DemoQuestionId(s.QIdx));
                if (info == null) continue;
                var topic = topics.GetValueOrDefault(info.TopicId);
                var subject = subjects.GetValueOrDefault(info.SubjectId);
                plan.Tasks.Add(new StudyTask
                {
                    Id = Guid.Parse($"d0000000-0000-0000-0000-0000000000{71 + day}"),
                    InstitutionId = DemoInstitutionId,
                    StudyPlanId = DemoStudyPlanId,
                    StudentId = DemoStudentId,
                    DayIndex = day,
                    PlannedDate = start.AddDays(day),
                    Sequence = 1,
                    DurationMinutes = 45,
                    NodeType = RecommendationNodeType.Topic,
                    NodeId = info.TopicId,
                    SubjectId = info.SubjectId,
                    SubjectName = subject?.Name,
                    TopicId = info.TopicId,
                    TopicName = topic?.Name,
                    TitleSnapshot = $"{subject?.Name} - {topic?.Name} konu tekrarı ve soru çözümü",
                    ClassificationStateSnapshot = s.State,
                    PriorityScoreSnapshot = s.Priority,
                    RecommendationType = s.Type,
                    IsCompleted = s.Done,
                    CompletedAt = s.Done ? start.AddDays(day).AddHours(18) : null
                });
                day++;
            }

            plan.TotalPlannedMinutes = plan.Tasks.Sum(t => t.DurationMinutes);
            plan.UnallocatedMinutes = plan.TotalAvailableMinutes - plan.TotalPlannedMinutes;
            plan.UtilizationRate = plan.TotalAvailableMinutes > 0
                ? Math.Round((decimal)plan.TotalPlannedMinutes / plan.TotalAvailableMinutes, 4)
                : 0m;

            _db.Set<StudyPlan>().Add(plan);
        }

        // 2. Student 9 (Gamze Doğan - Declining) Targeted Study Plan
        var s9Id = DemoStudentEntityGuid(9);
        if (!await _db.Set<StudyPlan>().IgnoreQueryFilters().AnyAsync(p => p.Id == DemoStudyPlanS9Id, ct) &&
            !await _db.Set<StudyPlan>().AnyAsync(p => p.StudentId == s9Id && p.Status == StudyPlanStatus.Active, ct))
        {
            var s9Specs = new (int QIdx, AcademicClassificationState State, int Priority, AcademicRecommendationType Type, bool Done)[]
            {
                (61, AcademicClassificationState.Critical, 95, AcademicRecommendationType.RemediateCritical, false),
                (81, AcademicClassificationState.Critical, 90, AcademicRecommendationType.RemediateCritical, false),
                (51, AcademicClassificationState.Weak, 82, AcademicRecommendationType.StrengthenWeak, false),
                (71, AcademicClassificationState.Weak, 76, AcademicRecommendationType.StrengthenWeak, true),
                (1, AcademicClassificationState.Developing, 60, AcademicRecommendationType.ReinforceDeveloping, true),
            };

            var plan9 = new StudyPlan
            {
                Id = DemoStudyPlanS9Id,
                InstitutionId = DemoInstitutionId,
                StudentId = s9Id,
                Title = "Fizik & Kimya Toparlanma Çalışma Planı",
                StartDate = start,
                EndDate = start.AddDays(7),
                Status = StudyPlanStatus.Active,
                Source = StudyPlanSource.AcademicIntelligence,
                RecommendationPolicyVersion = "demo-v1",
                StudyPlanPolicyVersion = "demo-v1",
                ActivatedAt = start,
                PlanDays = 7,
                DailyStudyMinutes = 60,
                SessionMinutes = 45,
                TotalAvailableMinutes = 420,
                ScheduledRecommendationCount = s9Specs.Length,
                UnscheduledRecommendationCount = 0
            };

            int day9 = 0;
            foreach (var s in s9Specs)
            {
                var info = topicInfo.FirstOrDefault(t => t.Id == DemoQuestionId(s.QIdx));
                if (info == null) continue;
                var topic = topics.GetValueOrDefault(info.TopicId);
                var subject = subjects.GetValueOrDefault(info.SubjectId);
                plan9.Tasks.Add(new StudyTask
                {
                    Id = Guid.Parse($"d0000000-0000-0000-0000-0000000000{91 + day9}"),
                    InstitutionId = DemoInstitutionId,
                    StudyPlanId = DemoStudyPlanS9Id,
                    StudentId = s9Id,
                    DayIndex = day9,
                    PlannedDate = start.AddDays(day9),
                    Sequence = 1,
                    DurationMinutes = 45,
                    NodeType = RecommendationNodeType.Topic,
                    NodeId = info.TopicId,
                    SubjectId = info.SubjectId,
                    SubjectName = subject?.Name,
                    TopicId = info.TopicId,
                    TopicName = topic?.Name,
                    TitleSnapshot = $"{subject?.Name} - {topic?.Name} toparlanma ve soru çözümü",
                    ClassificationStateSnapshot = s.State,
                    PriorityScoreSnapshot = s.Priority,
                    RecommendationType = s.Type,
                    IsCompleted = s.Done,
                    CompletedAt = s.Done ? start.AddDays(day9).AddHours(18) : null
                });
                day9++;
            }

            plan9.TotalPlannedMinutes = plan9.Tasks.Sum(t => t.DurationMinutes);
            plan9.UnallocatedMinutes = plan9.TotalAvailableMinutes - plan9.TotalPlannedMinutes;
            plan9.UtilizationRate = plan9.TotalAvailableMinutes > 0
                ? Math.Round((decimal)plan9.TotalPlannedMinutes / plan9.TotalAvailableMinutes, 4)
                : 0m;

            _db.Set<StudyPlan>().Add(plan9);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedNotesAsync(CancellationToken ct)
    {
        var notebook = new (Guid Id, string Title, string Content, string Tags, bool Pinned)[]
        {
            (Guid.Parse("d0000000-0000-0000-0000-000000000080"), "Trigonometri Formülleri",
                "sin²x + cos²x = 1, tan x = sin x / cos x. Yarım açı formüllerini her gün 10 dakika tekrar et.", "matematik,formül", true),
            (Guid.Parse("d0000000-0000-0000-0000-000000000081"), "Paragraf Stratejisi",
                "Önce soru kökünü oku, sonra paragrafı. Ana düşünce sorularında ilk ve son cümleye dikkat.", "türkçe,strateji", false),
        };
        foreach (var n in notebook)
        {
            if (!await _db.Set<NotebookEntry>().IgnoreQueryFilters().AnyAsync(x => x.Id == n.Id, ct))
            {
                _db.Set<NotebookEntry>().Add(new NotebookEntry
                {
                    Id = n.Id,
                    InstitutionId = DemoInstitutionId,
                    StudentId = DemoStudentId,
                    Title = n.Title,
                    Content = n.Content,
                    Tags = n.Tags,
                    IsPinned = n.Pinned
                });
            }
        }

        var coachNotes = new (Guid Id, Guid StudentId, string Category, string Content)[]
        {
            (Guid.Parse("d0000000-0000-0000-0000-000000000082"), DemoStudentId, "Akademik",
                "Matematikte istikrarlı yükseliş var (4 → 7 doğru). Trigonometri hâlâ kritik; haftalık planda önceliklendirildi."),
            (Guid.Parse("d0000000-0000-0000-0000-000000000083"), DemoStudentId, "Motivasyon",
                "Fizikte son denemelerde düşüş var; görüşmede sınav kaygısı ve çalışma düzeni konuşulacak."),
            (Guid.Parse("d0000000-0000-0000-0000-000000000084"), DemoStudentEntityGuid(9), "Akademik Risk",
                "Son 4 denemedir Fizik ve Kimya netlerinde düşüş sürüyor (Elektrik ve Kimyasal Türler kritik seviyede). Deneme kaygısı ve haftalık soru hedefi düşüklüğü tespit edildi; acil toparlanma planı oluşturuldu."),
            (Guid.Parse("d0000000-0000-0000-0000-000000000085"), DemoStudentEntityGuid(3), "Hedef Odaklı",
                "Tıp fakültesi hedefi doğrultusunda performansı mükemmel düzeyde (35+ net). Süre yönetimi ve AYT fen denemelerine erken geçiş planlandı."),
            (Guid.Parse("d0000000-0000-0000-0000-000000000086"), DemoStudentEntityGuid(10), "Çalışma Düzeni",
                "Deneme netleri 18 ile 28 arasında dalgalanıyor. Çalışma saatlerinin düzensiz olduğu görüldü; günlük soru takip çizelgesi uygulamaya alındı.")
        };
        foreach (var n in coachNotes)
        {
            if (!await _db.Set<CoachPrivateNote>().IgnoreQueryFilters().AnyAsync(x => x.Id == n.Id, ct))
            {
                _db.Set<CoachPrivateNote>().Add(new CoachPrivateNote
                {
                    Id = n.Id,
                    InstitutionId = DemoInstitutionId,
                    CoachId = DemoCoachEntityId,
                    StudentId = n.StudentId,
                    Category = n.Category,
                    Content = n.Content
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedMeetingsAsync(CancellationToken ct)
    {
        var coach1Id = DemoCoachEntityGuid(1);
        var coach2Id = DemoCoachEntityGuid(2);
        var coach3Id = DemoCoachEntityGuid(3);

        var student1Id = DemoStudentEntityGuid(1);
        var student9Id = DemoStudentEntityGuid(9);
        var student11Id = DemoStudentEntityGuid(11);
        var student21Id = DemoStudentEntityGuid(21);

        var today = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);

        var meetings = new[]
        {
            new CoachMeeting
            {
                Id = DemoMeeting1Id,
                InstitutionId = DemoInstitutionId,
                CoachId = coach1Id,
                StudentId = student1Id,
                StartTime = today.AddDays(-3).AddHours(14),
                DurationMinutes = 45,
                EndTime = today.AddDays(-3).AddHours(14).AddMinutes(45),
                MeetingType = "Haftalık Takip & Analiz",
                LocationType = "Online Video",
                Status = CoachMeetingStatus.Completed,
                Notes = "Deneme 9 analizi yapıldı. Matematik yükselişi takdir edildi; trigonometri çalışma planı başlatıldı."
            },
            new CoachMeeting
            {
                Id = DemoMeeting2Id,
                InstitutionId = DemoInstitutionId,
                CoachId = coach1Id,
                StudentId = student1Id,
                StartTime = today.AddDays(2).AddHours(15),
                DurationMinutes = 45,
                EndTime = today.AddDays(2).AddHours(15).AddMinutes(45),
                MeetingType = "Sınav Stratejisi & Hedef Belirleme",
                LocationType = "Online Video",
                Status = CoachMeetingStatus.Upcoming,
                Notes = "Deneme 10 netleri ve hedef üniversite puan aralıkları değerlendirilecek."
            },
            new CoachMeeting
            {
                Id = DemoMeeting3Id,
                InstitutionId = DemoInstitutionId,
                CoachId = coach1Id,
                StudentId = student9Id,
                StartTime = today.AddDays(1).AddHours(16),
                DurationMinutes = 30,
                EndTime = today.AddDays(1).AddHours(16).AddMinutes(30),
                MeetingType = "Akademik İyileştirme & Motivasyon",
                LocationType = "Yüz Yüze • Kurum",
                Status = CoachMeetingStatus.Upcoming,
                Notes = "Fizik netlerindeki düşüşün kök nedenleri ve çalışma takvimi planlaması."
            },
            new CoachMeeting
            {
                Id = DemoMeeting4Id,
                InstitutionId = DemoInstitutionId,
                CoachId = coach2Id,
                StudentId = student11Id,
                StartTime = today.AddDays(-4).AddHours(10),
                DurationMinutes = 45,
                EndTime = today.AddDays(-4).AddHours(10).AddMinutes(45),
                MeetingType = "Haftalık Takip & Analiz",
                LocationType = "Online Video",
                Status = CoachMeetingStatus.Completed,
                Notes = "Dalgalı deneme grafiği ve çalışma sürekliliği değerlendirildi."
            },
            new CoachMeeting
            {
                Id = DemoMeeting5Id,
                InstitutionId = DemoInstitutionId,
                CoachId = coach3Id,
                StudentId = student21Id,
                StartTime = today.AddDays(3).AddHours(11),
                DurationMinutes = 45,
                EndTime = today.AddDays(3).AddHours(11).AddMinutes(45),
                MeetingType = "Hedef Belirleme",
                LocationType = "Online Video",
                Status = CoachMeetingStatus.Upcoming,
                Notes = "Sözel bölüm tercihleri ve deneme zaman yönetimi."
            }
        };

        foreach (var m in meetings)
        {
            if (!await _db.Set<CoachMeeting>().IgnoreQueryFilters().AnyAsync(x => x.Id == m.Id, ct))
            {
                _db.Set<CoachMeeting>().Add(m);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedConversationsAndMessagesAsync(CancellationToken ct)
    {
        var coach1User = DemoCoachUserGuid(1);
        var teacher1User = DemoTeacherUserGuid(1);
        var student1User = DemoStudentUserGuid(1);
        var parent1User = DemoParentUserGuid(1);
        var student9User = DemoStudentUserGuid(9);

        var convSpecs = new[]
        {
            (
                Id: DemoConvCoachStudent1Id,
                UserA: coach1User,
                UserB: student1User,
                Subject: "Bireysel Koçluk Takibi",
                Messages: new[]
                {
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000001"), Sender: coach1User, Recipient: student1User, Content: "Merhaba Ahmet, son denemedeki Matematik artışını tebrik ederim. Trigonometri için eklediğim çalışma planını tamamlamayı unutma.", DaysAgo: 2),
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000002"), Sender: student1User, Recipient: coach1User, Content: "Teşekkürler hocam, trigonometri sorularına başladım. Takıldığım 2 soruyu bir sonraki görüşmemizde konuşabilir miyiz?", DaysAgo: 1),
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000003"), Sender: coach1User, Recipient: student1User, Content: "Tabii ki, haftalık takip görüşmemizde detaylıca inceleriz.", DaysAgo: 0)
                }
            ),
            (
                Id: DemoConvCoachParent1Id,
                UserA: coach1User,
                UserB: parent1User,
                Subject: "Ahmet Yılmaz Gelişim Raporu",
                Messages: new[]
                {
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000004"), Sender: coach1User, Recipient: parent1User, Content: "Fatma Hanım merhaba, Ahmet'in son deneme sonuçları ve gelişim grafiği oldukça olumlu. Çalışma temposunu koruyoruz.", DaysAgo: 3),
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000005"), Sender: parent1User, Recipient: coach1User, Content: "Mehmet Hocam bilgilendirme için teşekkürler. Evdeki çalışma saatlerine de özen gösteriyor.", DaysAgo: 2)
                }
            ),
            (
                Id: DemoConvTeacherStudent1Id,
                UserA: teacher1User,
                UserB: student1User,
                Subject: "Matematik Çalışma Fasikülü",
                Messages: new[]
                {
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000006"), Sender: teacher1User, Recipient: student1User, Content: "Ahmet, derste bahsettiğim Trigonometri fasikülünü materyaller kısmına yükledim. İnceleyip soruları çözebilirsin.", DaysAgo: 2),
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000007"), Sender: student1User, Recipient: teacher1User, Content: "Çok teşekkürler Ayşe Öğretmenim, hemen indirip çözmeye başlıyorum.", DaysAgo: 1)
                }
            ),
            (
                Id: DemoConvTeacherParent1Id,
                UserA: teacher1User,
                UserB: parent1User,
                Subject: "Matematik Dersi Katılımı",
                Messages: new[]
                {
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000008"), Sender: teacher1User, Recipient: parent1User, Content: "İyi günler Fatma Hanım, Ahmet'in Matematik dersindeki katılımı ve soru çözme isteği çok iyi seviyede.", DaysAgo: 4),
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000009"), Sender: parent1User, Recipient: teacher1User, Content: "Çok teşekkürler Ayşe Öğretmenim, desteğiniz bizim için çok kıymetli.", DaysAgo: 3)
                }
            ),
            (
                Id: DemoConvCoachStudent9Id,
                UserA: coach1User,
                UserB: student9User,
                Subject: "Akademik İyileştirme Planı",
                Messages: new[]
                {
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000010"), Sender: coach1User, Recipient: student9User, Content: "Gamze merhaba, Fizik ve Kimya konularındaki eksikleri toparlamak için bir çalışma planı hazırlayalım, görüşme saatimizi belirledim.", DaysAgo: 5),
                    (Id: Guid.Parse("d0000000-0000-0061-0000-000000000011"), Sender: student9User, Recipient: coach1User, Content: "Hocam biraz zorlanıyorum bu aralar, görüşmede planlama yaparsak çok sevinirim.", DaysAgo: 4)
                }
            )
        };

        foreach (var cs in convSpecs)
        {
            var p1 = cs.UserA.CompareTo(cs.UserB) < 0 ? cs.UserA : cs.UserB;
            var p2 = cs.UserA.CompareTo(cs.UserB) < 0 ? cs.UserB : cs.UserA;
            var lastMsg = cs.Messages.Last();
            var lastTime = DateTimeOffset.UtcNow.AddDays(-lastMsg.DaysAgo).AddHours(14);

            if (!await _db.Set<Conversation>().IgnoreQueryFilters().AnyAsync(c => c.Id == cs.Id, ct))
            {
                var conv = new Conversation
                {
                    Id = cs.Id,
                    InstitutionId = DemoInstitutionId,
                    Participant1Id = p1,
                    Participant2Id = p2,
                    Subject = cs.Subject,
                    LastMessageAt = lastTime,
                    LastMessageSnippet = lastMsg.Content
                };

                foreach (var m in cs.Messages)
                {
                    var mTime = DateTimeOffset.UtcNow.AddDays(-m.DaysAgo).AddHours(12);
                    conv.Messages.Add(new Message
                    {
                        Id = m.Id,
                        InstitutionId = DemoInstitutionId,
                        ConversationId = cs.Id,
                        SenderUserId = m.Sender,
                        RecipientUserId = m.Recipient,
                        Content = m.Content,
                        IsRead = m.DaysAgo > 0,
                        ReadAt = m.DaysAgo > 0 ? mTime.AddMinutes(15) : null
                    });
                }

                _db.Set<Conversation>().Add(conv);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static readonly byte[] SamplePdfBytes = System.Text.Encoding.ASCII.GetBytes(
        "%PDF-1.4\n" +
        "1 0 obj <</Type /Catalog /Pages 2 0 R>> endobj\n" +
        "2 0 obj <</Type /Pages /Kids [3 0 R] /Count 1>> endobj\n" +
        "3 0 obj <</Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources <<>>>> endobj\n" +
        "xref\n0 4\n0000000000 65535 f \n0000000009 00000 n \n0000000056 00000 n \n0000000111 00000 n \n" +
        "trailer <</Size 4 /Root 1 0 R>>\nstartxref\n185\n%%EOF\n"
    );

    private static void EnsureSamplePdfOnDisk(string relativeStoragePath, byte[] content)
    {
        var basePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Materials"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Api", "EgitimPlatform.Api", "Storage", "Materials"),
            Path.Combine(AppContext.BaseDirectory, "Storage", "Materials")
        };

        foreach (var bp in basePaths.Distinct())
        {
            try
            {
                var fullPath = Path.GetFullPath(Path.Combine(bp, relativeStoragePath));
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllBytes(fullPath, content);
            }
            catch
            {
                // Best effort write across test / production working directories
            }
        }
    }

    private async Task SeedMaterialsAsync(CancellationToken ct)
    {
        var teacher1Id = DemoTeacherEntityGuid(1);
        var teacher3Id = DemoTeacherEntityGuid(3);

        var mat1Storage = $"inst_{DemoInstitutionId:N}/teacher_{teacher1Id:N}/d0000000000000400000000000000001.pdf";
        var mat2Storage = $"inst_{DemoInstitutionId:N}/teacher_{teacher3Id:N}/d0000000000000400000000000000002.pdf";

        EnsureSamplePdfOnDisk(mat1Storage, SamplePdfBytes);
        EnsureSamplePdfOnDisk(mat2Storage, SamplePdfBytes);

        if (!await _db.Set<ContentMaterial>().IgnoreQueryFilters().AnyAsync(m => m.Id == DemoMaterial1Id, ct))
        {
            var mat1 = new ContentMaterial
            {
                Id = DemoMaterial1Id,
                InstitutionId = DemoInstitutionId,
                TeacherId = teacher1Id,
                Title = "TYT Matematik — Fonksiyonlar ve Trigonometri Çalışma Fasikülü",
                Subject = "Matematik",
                Topic = "Trigonometri",
                Grade = "12",
                MaterialType = "fasikul",
                Description = "12. Sınıf TYT/AYT hazırlık için kapsamlı trigonometri konu özeti ve yeni nesil soru fasikülü.",
                PageCount = 24,
                QuestionCount = 60,
                DurationMinutes = 90,
                OriginalFileName = "TYT_Matematik_Trigonometri_Fasikulu.pdf",
                StoragePath = mat1Storage,
                ContentType = "application/pdf",
                FileSizeBytes = SamplePdfBytes.Length
            };

            mat1.Assignments.Add(new ContentMaterialAssignment
            {
                Id = DemoMaterialAssignment1Id,
                InstitutionId = DemoInstitutionId,
                MaterialId = DemoMaterial1Id,
                ClassroomId = DemoClassroom12AId,
                Note = "12-A sınıfı hafta sonu çalışma ödevi",
                IsActive = true
            });

            mat1.Assignments.Add(new ContentMaterialAssignment
            {
                Id = DemoMaterialAssignment2Id,
                InstitutionId = DemoInstitutionId,
                MaterialId = DemoMaterial1Id,
                ClassroomId = DemoClassroom12BId,
                Note = "12-B sınıfı trigonometri pekiştirme fasikülü",
                IsActive = true
            });

            _db.Set<ContentMaterial>().Add(mat1);
        }

        if (!await _db.Set<ContentMaterial>().IgnoreQueryFilters().AnyAsync(m => m.Id == DemoMaterial2Id, ct))
        {
            var mat2 = new ContentMaterial
            {
                Id = DemoMaterial2Id,
                InstitutionId = DemoInstitutionId,
                TeacherId = teacher3Id,
                Title = "TYT Fizik — Elektrik Devreleri ve Manyetizma Ders Notları",
                Subject = "Fizik",
                Topic = "Elektrik",
                Grade = "12",
                MaterialType = "fasikul",
                Description = "Temel elektrik devreleri, eşdeğer direnç ve manyetizma özetli soru föyü.",
                PageCount = 16,
                QuestionCount = 40,
                DurationMinutes = 60,
                OriginalFileName = "TYT_Fizik_Elektrik_Ders_Notlari.pdf",
                StoragePath = mat2Storage,
                ContentType = "application/pdf",
                FileSizeBytes = SamplePdfBytes.Length
            };

            mat2.Assignments.Add(new ContentMaterialAssignment
            {
                Id = DemoMaterialAssignment3Id,
                InstitutionId = DemoInstitutionId,
                MaterialId = DemoMaterial2Id,
                ClassroomId = DemoClassroom12AId,
                Note = "12-A sınıfı fizik etüt materyali",
                IsActive = true
            });

            mat2.Assignments.Add(new ContentMaterialAssignment
            {
                Id = DemoMaterialAssignment4Id,
                InstitutionId = DemoInstitutionId,
                MaterialId = DemoMaterial2Id,
                ClassroomId = DemoClassroom12CId,
                Note = "12-C sınıfı fizik konu özeti",
                IsActive = true
            });

            _db.Set<ContentMaterial>().Add(mat2);
        }

        await _db.SaveChangesAsync(ct);
    }

    private class DemoTaxonomyContainer
    {
        public Guid ExamTypeId { get; set; }
        public Subject SubMat { get; set; } = null!;
        public Subject SubFiz { get; set; } = null!;
        public Subject SubTur { get; set; } = null!;
        public Subject SubKim { get; set; } = null!;
        public Subject SubBiy { get; set; } = null!;

        public Topic TopMatTk { get; set; } = null!;
        public Topic TopMatPrb { get; set; } = null!;
        public Topic TopMatFonk { get; set; } = null!;
        public Topic TopMatTrig { get; set; } = null!;

        public Topic TopFizElek { get; set; } = null!;
        public Topic TopFizHk { get; set; } = null!;

        public Topic TopTurPar { get; set; } = null!;
        public Topic TopTurDbg { get; set; } = null!;

        public Topic TopKimKt { get; set; } = null!;
        public Topic TopBiyHuc { get; set; } = null!;
    }
}
