using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Communication.Entities;
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

        // 5. 5 Finalized Demo Exams & Attempts
        await SeedExamsAndAttemptsAsync(taxonomy, questions, ct);

        // 6. Historical Coach Task
        await SeedCoachTasksAsync(ct);

        // 7. Notifications
        await SeedNotificationsAsync(ct);

        // 8. Linked classrooms, question-level data, study plan and notes
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
        var questionResults = await _db.Set<StudentExamQuestionResult>().IgnoreQueryFilters().Where(r => r.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudentExamQuestionResult>().RemoveRange(questionResults);

        var examQuestions = await _db.Set<ExamQuestion>().IgnoreQueryFilters().Where(q => q.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<ExamQuestion>().RemoveRange(examQuestions);

        var studyTasks = await _db.Set<StudyTask>().IgnoreQueryFilters().Where(t => t.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudyTask>().RemoveRange(studyTasks);

        var studyPlans = await _db.Set<StudyPlan>().IgnoreQueryFilters().Where(p => p.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudyPlan>().RemoveRange(studyPlans);

        var notebookEntries = await _db.Set<NotebookEntry>().IgnoreQueryFilters().Where(n => n.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<NotebookEntry>().RemoveRange(notebookEntries);

        var coachNotes = await _db.Set<CoachPrivateNote>().IgnoreQueryFilters().Where(n => n.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<CoachPrivateNote>().RemoveRange(coachNotes);

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

        var attempts = await _db.Set<StudentExamAttempt>().IgnoreQueryFilters().Where(a => a.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<StudentExamAttempt>().RemoveRange(attempts);

        var exams = await _db.Set<Exam>().IgnoreQueryFilters().Where(e => e.InstitutionId == DemoInstitutionId).ToListAsync(ct);
        _db.Set<Exam>().RemoveRange(exams);

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

        var userIds = new[] { DemoAdminUserId, DemoTeacherUserId, DemoCoachUserId, DemoStudentUserId, DemoParentUserId };
        foreach (var uid in userIds)
        {
            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user != null) await _userManager.DeleteAsync(user);
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

    private async Task SeedExamsAndAttemptsAsync(DemoTaxonomyContainer tax, List<Question> questions, CancellationToken ct)
    {
        var examDates = new (string Title, DateTimeOffset Date, Guid ExamId, Guid AttemptId)[]
        {
            ("Demo TYT 1", new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000011"), Guid.Parse("d0000000-0000-0000-0000-000000000021")),
            ("Demo TYT 2", new DateTimeOffset(2026, 9, 13, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000012"), Guid.Parse("d0000000-0000-0000-0000-000000000022")),
            ("Demo TYT 3", new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000013"), Guid.Parse("d0000000-0000-0000-0000-000000000023")),
            ("Demo TYT 4", new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000014"), Guid.Parse("d0000000-0000-0000-0000-000000000024")),
            ("Demo TYT 5", new DateTimeOffset(2026, 10, 4, 10, 0, 0, TimeSpan.FromHours(3)), Guid.Parse("d0000000-0000-0000-0000-000000000015"), Guid.Parse("d0000000-0000-0000-0000-000000000025")),
        };

        for (int idx = 0; idx < examDates.Length; idx++)
        {
            var eInfo = examDates[idx];
            var exam = await _db.Set<Exam>().IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == eInfo.ExamId, ct);
            if (exam == null)
            {
                exam = new Exam
                {
                    Id = eInfo.ExamId,
                    InstitutionId = DemoInstitutionId,
                    ExamTypeId = tax.ExamTypeId,
                    Code = $"TYT-2026-0{idx + 1}",
                    Title = eInfo.Title,
                    ExamDate = eInfo.Date,
                    Status = ExamStatus.Published,
                    WrongAnswerPenalty = 4m
                };

                var secTur = new ExamSection { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubTur.Id, DisplayOrder = 1, QuestionCount = 10 };
                var secMat = new ExamSection { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubMat.Id, DisplayOrder = 2, QuestionCount = 10 };
                var secFiz = new ExamSection { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubFiz.Id, DisplayOrder = 3, QuestionCount = 10 };
                var secKim = new ExamSection { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubKim.Id, DisplayOrder = 4, QuestionCount = 10 };
                var secBiy = new ExamSection { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, ExamId = exam.Id, SubjectId = tax.SubBiy.Id, DisplayOrder = 5, QuestionCount = 0 };

                exam.Sections.AddRange([secTur, secMat, secFiz, secKim, secBiy]);
                _db.Set<Exam>().Add(exam);
                await _db.SaveChangesAsync(ct);
            }

            var attempt = await _db.Set<StudentExamAttempt>().IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == eInfo.AttemptId, ct);
            if (attempt == null)
            {
                attempt = new StudentExamAttempt
                {
                    Id = eInfo.AttemptId,
                    InstitutionId = DemoInstitutionId,
                    ExamId = exam.Id,
                    StudentId = DemoStudentId,
                    AttemptNumber = 1,
                    TakenAt = eInfo.Date,
                    Status = AttemptStatus.Finalized,
                    FinalizedAt = eInfo.Date.AddHours(3),
                    IsRawAvailable = true
                };

                int turCorrect = idx < 2 ? 7 : 8;
                int turWrong = 10 - turCorrect;

                int matCorrect = idx == 0 ? 4 : idx == 1 ? 4 : idx == 2 ? 5 : idx == 3 ? 6 : 7;
                int matWrong = 10 - matCorrect;

                int fizCorrect = idx == 0 ? 7 : idx == 1 ? 6 : idx == 2 ? 6 : idx == 3 ? 5 : 4;
                int fizWrong = 10 - fizCorrect;

                int kimCorrect = idx == 0 ? 5 : 6;
                int kimWrong = 10 - kimCorrect;

                int totalCorrect = turCorrect + matCorrect + fizCorrect + kimCorrect;
                int totalWrong = turWrong + matWrong + fizWrong + kimWrong;
                decimal totalNet = totalCorrect - (totalWrong / 4m);

                attempt.TotalCorrect = totalCorrect;
                attempt.TotalWrong = totalWrong;
                attempt.TotalBlank = 0;
                attempt.TotalNet = totalNet;

                attempt.SubjectResults = new List<StudentExamSubjectResult>
                {
                    new StudentExamSubjectResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[0].Id, Correct = turCorrect, Wrong = turWrong, Blank = 0, Net = turCorrect - (turWrong / 4m) },
                    new StudentExamSubjectResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[1].Id, Correct = matCorrect, Wrong = matWrong, Blank = 0, Net = matCorrect - (matWrong / 4m) },
                    new StudentExamSubjectResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[2].Id, Correct = fizCorrect, Wrong = fizWrong, Blank = 0, Net = fizCorrect - (fizWrong / 4m) },
                    new StudentExamSubjectResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[3].Id, Correct = kimCorrect, Wrong = kimWrong, Blank = 0, Net = kimCorrect - (kimWrong / 4m) },
                };

                int matTkCorrect = 2;
                int matPrbCorrect = 1;
                int matFonkCorrect = idx < 2 ? 1 : 2;
                int matTrigCorrect = idx == 4 ? 1 : 0;

                int fizElekCorrect = idx == 0 ? 3 : idx < 3 ? 2 : 0;
                int fizHkCorrect = fizCorrect - fizElekCorrect;
                if (fizHkCorrect < 0) fizHkCorrect = 0;

                attempt.TopicResults = new List<StudentExamTopicResult>
                {
                    new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[0].Id, TopicId = tax.TopTurPar.Id, Correct = turCorrect / 2, Wrong = (10 - turCorrect) / 2, Blank = 0, Net = (turCorrect / 2m) },
                    new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[0].Id, TopicId = tax.TopTurDbg.Id, Correct = turCorrect - (turCorrect / 2), Wrong = 0, Blank = 0, Net = turCorrect - (turCorrect / 2m) },
                    new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[1].Id, TopicId = tax.TopMatTk.Id, Correct = matTkCorrect, Wrong = 0, Blank = 0, Net = matTkCorrect },
                    new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[1].Id, TopicId = tax.TopMatPrb.Id, Correct = matPrbCorrect, Wrong = 1, Blank = 0, Net = matPrbCorrect - 0.25m },
                    new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[1].Id, TopicId = tax.TopMatFonk.Id, Correct = matFonkCorrect, Wrong = 1, Blank = 0, Net = matFonkCorrect - 0.25m },
                    new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[1].Id, TopicId = tax.TopMatTrig.Id, Correct = matTrigCorrect, Wrong = 2, Blank = 0, Net = matTrigCorrect - 0.5m },
                    new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[2].Id, TopicId = tax.TopFizElek.Id, Correct = fizElekCorrect, Wrong = 5 - fizElekCorrect, Blank = 0, Net = fizElekCorrect - ((5 - fizElekCorrect) / 4m) },
                    new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = DemoInstitutionId, StudentExamAttemptId = attempt.Id, ExamSectionId = exam.Sections[2].Id, TopicId = tax.TopFizHk.Id, Correct = fizHkCorrect, Wrong = 5 - fizHkCorrect, Blank = 0, Net = fizHkCorrect - ((5 - fizHkCorrect) / 4m) },
                };

                _db.Set<StudentExamAttempt>().Add(attempt);
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    private async Task SeedCoachTasksAsync(CancellationToken ct)
    {
        var taskId = Guid.Parse("d0000000-0000-0000-0000-000000000040");
        if (!await _db.Set<CoachTask>().IgnoreQueryFilters().AnyAsync(t => t.Id == taskId, ct))
        {
            _db.Set<CoachTask>().Add(new CoachTask
            {
                Id = taskId,
                InstitutionId = DemoInstitutionId,
                StudentId = DemoStudentId,
                CoachUserId = DemoCoachUserId,
                Title = "Paragraf Tekrar Çalışması",
                Description = "Son deneme sonucuna göre Türkçe Paragraf soruları üzerinde 30 soru çözümü.",
                DueDate = DateTimeOffset.UtcNow.AddDays(-1),
                DurationMinutes = 45,
                SubjectName = "Türkçe",
                TopicName = "Paragraf",
                Status = CoachTaskStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow.AddDays(-2)
            });
            await _db.SaveChangesAsync(ct);
        }
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
    // Adds linked classrooms, a second student, question-level/style-tagged exam data,
    // an active study plan and sample notes. Every record is scoped to DemoInstitutionId
    // and uses deterministic IDs so repeated runs never duplicate data.

    public static readonly Guid DemoStudent2Id = Guid.Parse("d0000000-0000-0000-0000-00000000000d");
    public static readonly Guid DemoClassroom12AId = Guid.Parse("d0000000-0000-0000-0000-000000000060");
    public static readonly Guid DemoClassroom12BId = Guid.Parse("d0000000-0000-0000-0000-000000000061");
    public static readonly Guid DemoStudyPlanId = Guid.Parse("d0000000-0000-0000-0000-000000000070");

    private async Task SeedShowcaseEnrichmentAsync(CancellationToken ct)
    {
        await SeedClassroomsAsync(ct);
        await SeedQuestionStyleAssignmentsAsync(ct);
        await SeedQuestionLevelResultsAsync(ct);
        await SeedStudyPlanAsync(ct);
        await SeedNotesAsync(ct);
    }

    private async Task SeedClassroomsAsync(CancellationToken ct)
    {
        if (!await _db.Set<Student>().IgnoreQueryFilters().AnyAsync(s => s.Id == DemoStudent2Id, ct))
        {
            _db.Set<Student>().Add(new Student
            {
                Id = DemoStudent2Id,
                InstitutionId = DemoInstitutionId,
                FirstName = "Zeynep",
                LastName = "Demir",
                StudentNumber = "1202",
                GradeLevel = 12,
                SchoolName = "Demo Anadolu Lisesi",
                EnrollmentDate = DateTimeOffset.UtcNow.AddMonths(-6),
                Status = StudentStatus.Active
            });
        }

        var classrooms = new[]
        {
            (Id: DemoClassroom12AId, Name: "12-A", Branch: "Sayısal"),
            (Id: DemoClassroom12BId, Name: "12-B", Branch: "Eşit Ağırlık")
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

        var teacherLinkId = Guid.Parse("d0000000-0000-0000-0000-000000000062");
        if (!await _db.Set<ClassroomTeacher>().IgnoreQueryFilters().AnyAsync(x => x.Id == teacherLinkId, ct))
        {
            _db.Set<ClassroomTeacher>().Add(new ClassroomTeacher
            {
                Id = teacherLinkId,
                InstitutionId = DemoInstitutionId,
                ClassroomId = DemoClassroom12AId,
                TeacherId = DemoTeacherEntityId,
                IsPrimary = true
            });
        }

        var studentLinks = new[]
        {
            (Id: Guid.Parse("d0000000-0000-0000-0000-000000000063"), StudentId: DemoStudentId),
            (Id: Guid.Parse("d0000000-0000-0000-0000-000000000064"), StudentId: DemoStudent2Id)
        };
        foreach (var l in studentLinks)
        {
            if (!await _db.Set<ClassroomStudent>().IgnoreQueryFilters().AnyAsync(x => x.Id == l.Id, ct))
            {
                _db.Set<ClassroomStudent>().Add(new ClassroomStudent
                {
                    Id = l.Id,
                    InstitutionId = DemoInstitutionId,
                    ClassroomId = DemoClassroom12AId,
                    StudentId = l.StudentId
                });
            }
        }

        var coachLink2Id = Guid.Parse("d0000000-0000-0000-0000-000000000065");
        if (!await _db.Set<StudentCoachAssignment>().IgnoreQueryFilters().AnyAsync(x => x.Id == coachLink2Id, ct))
        {
            _db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
            {
                Id = coachLink2Id,
                InstitutionId = DemoInstitutionId,
                StudentId = DemoStudent2Id,
                CoachId = DemoCoachEntityId,
                IsPrimary = true,
                IsActive = true,
                AssignedAt = DateTimeOffset.UtcNow.AddMonths(-2)
            });
        }

        // Teacher subject scope: Matematik (resolved via the demo question bank taxonomy)
        var matSubjectId = await _db.Set<Question>().IgnoreQueryFilters()
            .Where(q => q.Id == DemoQuestionId(21))
            .Select(q => (Guid?)q.SubjectId)
            .FirstOrDefaultAsync(ct);
        var teacherSubjectId = Guid.Parse("d0000000-0000-0000-0000-000000000066");
        if (matSubjectId.HasValue &&
            !await _db.Set<TeacherSubject>().IgnoreQueryFilters().AnyAsync(x => x.Id == teacherSubjectId, ct))
        {
            _db.Set<TeacherSubject>().Add(new TeacherSubject
            {
                Id = teacherSubjectId,
                InstitutionId = DemoInstitutionId,
                TeacherId = DemoTeacherEntityId,
                SubjectId = matSubjectId.Value
            });
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
        if (await _db.Set<StudyPlan>().IgnoreQueryFilters().AnyAsync(p => p.Id == DemoStudyPlanId, ct))
            return;
        // Respect the domain's single-active-plan rule.
        if (await _db.Set<StudyPlan>().AnyAsync(p => p.StudentId == DemoStudentId && p.Status == StudyPlanStatus.Active, ct))
            return;

        var topicIds = new[] { DemoQuestionId(51), DemoQuestionId(31), DemoQuestionId(61), DemoQuestionId(1), DemoQuestionId(41) };
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

        var coachNotes = new (Guid Id, string Category, string Content)[]
        {
            (Guid.Parse("d0000000-0000-0000-0000-000000000082"), "Akademik",
                "Matematikte istikrarlı yükseliş var (4 → 7 doğru). Trigonometri hâlâ kritik; haftalık planda önceliklendirildi."),
            (Guid.Parse("d0000000-0000-0000-0000-000000000083"), "Motivasyon",
                "Fizikte son denemelerde düşüş var; görüşmede sınav kaygısı ve çalışma düzeni konuşulacak."),
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
                    StudentId = DemoStudentId,
                    Category = n.Category,
                    Content = n.Content
                });
            }
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
