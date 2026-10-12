using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Exams.Entities;
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
            _logger.LogInformation("Demo showcase dataset (DEMO_SHOWCASE_V1) already exists. Skipping duplicate seeding.");
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
        if (!await _db.Set<StudentParent>().IgnoreQueryFilters().AnyAsync(sp => sp.StudentId == DemoStudentId && sp.ParentId == DemoParentUserId, ct))
        {
            _db.Set<StudentParent>().Add(new StudentParent
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-00000000000b"),
                InstitutionId = DemoInstitutionId,
                StudentId = DemoStudentId,
                ParentId = DemoParentUserId,
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
        if (!await _db.Set<StudentCoachAssignment>().IgnoreQueryFilters().AnyAsync(ca => ca.StudentId == DemoStudentId && ca.CoachId == DemoCoachUserId, ct))
        {
            _db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
            {
                Id = Guid.Parse("d0000000-0000-0000-0000-00000000000c"),
                InstitutionId = DemoInstitutionId,
                StudentId = DemoStudentId,
                CoachId = DemoCoachUserId,
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
