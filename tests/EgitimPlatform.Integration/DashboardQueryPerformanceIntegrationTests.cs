using System.Data;
using System.Diagnostics;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features.GetInstitutionOverview;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using EgitimPlatform.Modules.Teachers.Features.GetTeacherDashboard;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class DashboardQueryPerformanceIntegrationTests
{
    private readonly IntegrationTestFactory _factory;
    private readonly ITestOutputHelper _output;

    public DashboardQueryPerformanceIntegrationTests(IntegrationTestFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    private sealed class SqlCommandCounter : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private static readonly AsyncLocal<SqlCommandCounter?> Current = new();
        private readonly IDisposable _subscription;
        private readonly List<IDisposable> _listenerSubscriptions = [];
        public int Count { get; private set; }

        public SqlCommandCounter()
        {
            Current.Value = this;
            _subscription = DiagnosticListener.AllListeners.Subscribe(this);
        }

        public void Reset() => Count = 0;

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == "SqlClientDiagnosticListener" || value.Name == "Microsoft.EntityFrameworkCore")
            {
                _listenerSubscriptions.Add(value.Subscribe(this));
            }
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (Current.Value != this) return;
            if (value.Key == "Microsoft.Data.SqlClient.WriteCommandBefore")
            {
                Count++;
            }
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }

        public void Dispose()
        {
            Current.Value = null;
            foreach (var sub in _listenerSubscriptions) sub.Dispose();
            _subscription.Dispose();
        }
    }

    private static ICurrentUser CreateAdminUser(Guid institutionId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(Guid.NewGuid());
        user.IsSuperAdmin.Returns(false);
        user.Roles.Returns([Roles.InstitutionAdmin]);
        user.IsInRole(Roles.InstitutionAdmin).Returns(true);
        user.GetInstitutionIdAsync().Returns(Task.FromResult<Guid?>(institutionId));
        return user;
    }

    private static ICurrentUser CreateTeacherUser(Guid userId, Guid institutionId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(userId);
        user.IsSuperAdmin.Returns(false);
        user.Roles.Returns([Roles.Teacher]);
        user.IsInRole(Roles.Teacher).Returns(true);
        user.GetInstitutionIdAsync().Returns(Task.FromResult<Guid?>(institutionId));
        return user;
    }

    [Fact]
    public async Task InstitutionOverview_And_TeacherDashboard_MatchIndependentSqlReference_And_MeasurePerformance()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var academicCatalog = scope.ServiceProvider.GetRequiredService<IAcademicCatalog>();

        var instMain = new Institution { Id = Guid.NewGuid(), Name = $"Performans Kurumu {Guid.NewGuid():N}"[..25] };
        var instOther = new Institution { Id = Guid.NewGuid(), Name = $"Izole Kurum {Guid.NewGuid():N}"[..22] };
        db.Set<Institution>().AddRange(instMain, instOther);

        var examType = new ExamType
        {
            Id = Guid.NewGuid(),
            Code = $"TYTP_{Guid.NewGuid():N}"[..12],
            Name = "TYT Performans"
        };
        db.Set<ExamType>().Add(examType);

        var subTurkce = new Subject { Id = Guid.NewGuid(), ExamTypeId = examType.Id, Code = $"TR_{Guid.NewGuid():N}"[..10], Name = "Türkçe" };
        var subMat = new Subject { Id = Guid.NewGuid(), ExamTypeId = examType.Id, Code = $"MT_{Guid.NewGuid():N}"[..10], Name = "Matematik" };
        var subFiz = new Subject { Id = Guid.NewGuid(), ExamTypeId = examType.Id, Code = $"FZ_{Guid.NewGuid():N}"[..10], Name = "Fizik" };
        var subKim = new Subject { Id = Guid.NewGuid(), ExamTypeId = examType.Id, Code = $"KM_{Guid.NewGuid():N}"[..10], Name = "Kimya" };
        var subTar = new Subject { Id = Guid.NewGuid(), ExamTypeId = examType.Id, Code = $"TH_{Guid.NewGuid():N}"[..10], Name = "Tarih" };
        var subCog = new Subject { Id = Guid.NewGuid(), ExamTypeId = examType.Id, Code = $"CG_{Guid.NewGuid():N}"[..10], Name = "Coğrafya" };
        db.Set<Subject>().AddRange(subTurkce, subMat, subFiz, subKim, subTar, subCog);

        var topics = new List<Topic>
        {
            new() { Id = Guid.NewGuid(), SubjectId = subMat.Id, Code = $"T_M1_{Guid.NewGuid():N}"[..12], Name = "Türev" },
            new() { Id = Guid.NewGuid(), SubjectId = subMat.Id, Code = $"T_M2_{Guid.NewGuid():N}"[..12], Name = "İntegral" },
            new() { Id = Guid.NewGuid(), SubjectId = subMat.Id, Code = $"T_M3_{Guid.NewGuid():N}"[..12], Name = "Logaritma" },
            new() { Id = Guid.NewGuid(), SubjectId = subMat.Id, Code = $"T_M4_{Guid.NewGuid():N}"[..12], Name = "Fonksiyonlar" },
            new() { Id = Guid.NewGuid(), SubjectId = subFiz.Id, Code = $"T_F1_{Guid.NewGuid():N}"[..12], Name = "Optik" },
            new() { Id = Guid.NewGuid(), SubjectId = subFiz.Id, Code = $"T_F2_{Guid.NewGuid():N}"[..12], Name = "Dalgalar" },
            new() { Id = Guid.NewGuid(), SubjectId = subFiz.Id, Code = $"T_F3_{Guid.NewGuid():N}"[..12], Name = "Manyetizma" },
            new() { Id = Guid.NewGuid(), SubjectId = subFiz.Id, Code = $"T_F4_{Guid.NewGuid():N}"[..12], Name = "Dinamik" }
        };
        db.Set<Topic>().AddRange(topics);

        // Classrooms in instMain
        var class12A = new Classroom { Id = Guid.NewGuid(), InstitutionId = instMain.Id, Name = "12-A Sayısal", GradeLevel = 12, AcademicYear = "2026-2027" };
        var class12B = new Classroom { Id = Guid.NewGuid(), InstitutionId = instMain.Id, Name = "12-B Sayısal", GradeLevel = 12, AcademicYear = "2026-2027" };
        var classUnassigned = new Classroom { Id = Guid.NewGuid(), InstitutionId = instMain.Id, Name = "11-C Eşit Ağırlık", GradeLevel = 11, AcademicYear = "2026-2027" };
        db.Set<Classroom>().AddRange(class12A, class12B, classUnassigned);

        // Teacher in instMain assigned to 12-A and 12-B (NOT 11-C)
        var teacherUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"tperf-{Guid.NewGuid():N}@test.local",
            NormalizedUserName = $"TPERF-{Guid.NewGuid():N}@TEST.LOCAL",
            Email = $"tperf-{Guid.NewGuid():N}@test.local",
            NormalizedEmail = $"TPERF-{Guid.NewGuid():N}@TEST.LOCAL",
            FirstName = "Selim",
            LastName = "Yıldız",
            InstitutionId = instMain.Id,
            IsActive = true
        };
        var coachUser1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"c1perf-{Guid.NewGuid():N}@test.local",
            NormalizedUserName = $"C1PERF-{Guid.NewGuid():N}@TEST.LOCAL",
            Email = $"c1perf-{Guid.NewGuid():N}@test.local",
            NormalizedEmail = $"C1PERF-{Guid.NewGuid():N}@TEST.LOCAL",
            FirstName = "Elif",
            LastName = "Koç",
            InstitutionId = instMain.Id,
            IsActive = true
        };
        var coachUser2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"c2perf-{Guid.NewGuid():N}@test.local",
            NormalizedUserName = $"C2PERF-{Guid.NewGuid():N}@TEST.LOCAL",
            Email = $"c2perf-{Guid.NewGuid():N}@test.local",
            NormalizedEmail = $"C2PERF-{Guid.NewGuid():N}@TEST.LOCAL",
            FirstName = "Murat",
            LastName = "Koç",
            InstitutionId = instMain.Id,
            IsActive = true
        };
        db.Set<ApplicationUser>().AddRange(teacherUser, coachUser1, coachUser2);

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = instMain.Id,
            UserId = teacherUser.Id,
            FirstName = "Selim",
            LastName = "Yıldız",
            Title = "Başöğretmen"
        };
        db.Set<Teacher>().Add(teacher);
        db.Set<TeacherSubject>().AddRange(
            new TeacherSubject { Id = Guid.NewGuid(), InstitutionId = instMain.Id, TeacherId = teacher.Id, SubjectId = subMat.Id },
            new TeacherSubject { Id = Guid.NewGuid(), InstitutionId = instMain.Id, TeacherId = teacher.Id, SubjectId = subFiz.Id }
        );
        db.Set<ClassroomTeacher>().AddRange(
            new ClassroomTeacher { Id = Guid.NewGuid(), InstitutionId = instMain.Id, ClassroomId = class12A.Id, TeacherId = teacher.Id },
            new ClassroomTeacher { Id = Guid.NewGuid(), InstitutionId = instMain.Id, ClassroomId = class12B.Id, TeacherId = teacher.Id }
        );

        var coach1 = new Coach { Id = Guid.NewGuid(), InstitutionId = instMain.Id, UserId = coachUser1.Id, FirstName = "Elif", LastName = "Koç" };
        var coach2 = new Coach { Id = Guid.NewGuid(), InstitutionId = instMain.Id, UserId = coachUser2.Id, FirstName = "Murat", LastName = "Koç" };
        db.Set<Coach>().AddRange(coach1, coach2);

        // 25 active students in instMain:
        // - Students 0..4 (5 students) enrolled in BOTH 12-A and 12-B (multi-classroom students!)
        // - Students 5..14 (10 students) enrolled in 12-A only
        // - Students 15..19 (5 students) enrolled in 12-B only
        // - Students 20..24 (5 students) enrolled in 11-C (NOT taught by Selim Yıldız)
        var students = new List<Student>();
        var now = new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

        for (int i = 0; i < 25; i++)
        {
            var st = new Student
            {
                Id = Guid.NewGuid(),
                InstitutionId = instMain.Id,
                FirstName = $"Öğrenci{i:D2}",
                LastName = $"Soyad{i:D2}",
                StudentNumber = $"50{i:D2}",
                GradeLevel = i < 20 ? 12 : 11
            };
            students.Add(st);
            db.Set<Student>().Add(st);

            if (i < 5)
            {
                db.Set<ClassroomStudent>().Add(new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instMain.Id, ClassroomId = class12A.Id, StudentId = st.Id });
                db.Set<ClassroomStudent>().Add(new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instMain.Id, ClassroomId = class12B.Id, StudentId = st.Id });
            }
            else if (i < 15)
            {
                db.Set<ClassroomStudent>().Add(new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instMain.Id, ClassroomId = class12A.Id, StudentId = st.Id });
            }
            else if (i < 20)
            {
                db.Set<ClassroomStudent>().Add(new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instMain.Id, ClassroomId = class12B.Id, StudentId = st.Id });
            }
            else
            {
                db.Set<ClassroomStudent>().Add(new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instMain.Id, ClassroomId = classUnassigned.Id, StudentId = st.Id });
            }

            // Coach assignment & active + historical study plans
            var assignedCoach = i < 15 ? coach1 : coach2;
            db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
            {
                Id = Guid.NewGuid(),
                InstitutionId = instMain.Id,
                CoachId = assignedCoach.Id,
                StudentId = st.Id,
                IsActive = true,
                IsPrimary = true,
                AssignedAt = now.AddDays(-30)
            });

            decimal utilization = i < 15 ? 0.82m : 0.64m;
            int plannedMins = i < 15 ? 1260 : 900;
            db.Set<StudyPlan>().AddRange(
                new StudyPlan
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = instMain.Id,
                    StudentId = st.Id,
                    Title = $"Active Plan {i}",
                    StartDate = now.AddDays(-7),
                    EndDate = now.AddDays(7),
                    Status = StudyPlanStatus.Active,
                    UtilizationRate = utilization,
                    TotalPlannedMinutes = plannedMins,
                    RecommendationPolicyVersion = "v1",
                    StudyPlanPolicyVersion = "v1"
                },
                new StudyPlan
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = instMain.Id,
                    StudentId = st.Id,
                    Title = $"Completed Plan {i}",
                    StartDate = now.AddDays(-30),
                    EndDate = now.AddDays(-15),
                    Status = StudyPlanStatus.Completed,
                    UtilizationRate = 0.20m,
                    TotalPlannedMinutes = 300,
                    RecommendationPolicyVersion = "v1",
                    StudyPlanPolicyVersion = "v1"
                }
            );
        }

        // 6 Exams across 4 months (Jul, Aug, Sep, Oct 2026) -> 25 students * 6 exams = 150 attempts, 900 subject results, 1200 topic results
        var examDates = new[]
        {
            new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 12, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 5, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 3, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 14, 9, 0, 0, TimeSpan.Zero)
        };

        var allSubjects = new[] { subTurkce, subMat, subFiz, subKim, subTar, subCog };

        for (int eIdx = 0; eIdx < examDates.Length; eIdx++)
        {
            var exam = new Exam
            {
                Id = Guid.NewGuid(),
                InstitutionId = instMain.Id,
                ExamTypeId = examType.Id,
                Code = $"EX-P{eIdx}-{Guid.NewGuid():N}"[..14],
                Title = $"Performans Deneme {eIdx + 1}",
                ExamDate = examDates[eIdx],
                Status = ExamStatus.Published
            };
            db.Set<Exam>().Add(exam);

            var sectionMap = new Dictionary<Guid, ExamSection>();
            for (int sIdx = 0; sIdx < allSubjects.Length; sIdx++)
            {
                var sec = new ExamSection
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = instMain.Id,
                    ExamId = exam.Id,
                    SubjectId = allSubjects[sIdx].Id,
                    DisplayOrder = sIdx + 1,
                    QuestionCount = 20
                };
                sectionMap[allSubjects[sIdx].Id] = sec;
                db.Set<ExamSection>().Add(sec);
            }

            for (int stIdx = 0; stIdx < students.Count; stIdx++)
            {
                var st = students[stIdx];
                var attempt = new StudentExamAttempt
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = instMain.Id,
                    ExamId = exam.Id,
                    StudentId = st.Id,
                    AttemptNumber = 1,
                    TakenAt = examDates[eIdx].AddMinutes(stIdx),
                    Status = AttemptStatus.Finalized,
                    TotalCorrect = 70 + eIdx,
                    TotalWrong = 12,
                    TotalBlank = 18,
                    TotalNet = 67.0m + eIdx,
                    ReportedScore = 380.0m + eIdx * 5
                };
                db.Set<StudentExamAttempt>().Add(attempt);

                foreach (var sub in allSubjects)
                {
                    var baseNet = sub.Id == subMat.Id
                        ? 20.0m + eIdx * 1.5m + (stIdx % 5)
                        : sub.Id == subFiz.Id
                            ? 10.0m + eIdx * 0.5m
                            : 12.0m + eIdx;

                    db.Set<StudentExamSubjectResult>().Add(new StudentExamSubjectResult
                    {
                        Id = Guid.NewGuid(),
                        InstitutionId = instMain.Id,
                        StudentExamAttemptId = attempt.Id,
                        ExamSectionId = sectionMap[sub.Id].Id,
                        Correct = (int)Math.Round(baseNet) + 1,
                        Wrong = 4,
                        Blank = 2,
                        Net = baseNet
                    });
                }

                // 8 Topic results per attempt (4 Matematik, 4 Fizik)
                for (int tIdx = 0; tIdx < topics.Count; tIdx++)
                {
                    var top = topics[tIdx];
                    var secId = sectionMap[top.SubjectId].Id;
                    int wrongCount = (stIdx == 0 && tIdx == 0) ? 4 : (stIdx % 3 == 0 && tIdx < 2) ? 2 : 1;
                    int correctCount = 4;
                    decimal topicNet = correctCount - (wrongCount * 0.25m) + (eIdx * 0.5m);

                    db.Set<StudentExamTopicResult>().Add(new StudentExamTopicResult
                    {
                        Id = Guid.NewGuid(),
                        InstitutionId = instMain.Id,
                        StudentExamAttemptId = attempt.Id,
                        ExamSectionId = secId,
                        TopicId = top.Id,
                        Correct = correctCount,
                        Wrong = wrongCount,
                        Blank = 0,
                        Net = topicNet
                    });
                }
            }
        }

        // Competing institution records (for tenant isolation check)
        var otherStudent = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instOther.Id,
            FirstName = "Başka",
            LastName = "Kurum",
            StudentNumber = "9001"
        };
        db.Set<Student>().Add(otherStudent);
        db.Set<StudyPlan>().Add(new StudyPlan
        {
            Id = Guid.NewGuid(),
            InstitutionId = instOther.Id,
            StudentId = otherStudent.Id,
            Title = "Other Plan",
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(5),
            Status = StudyPlanStatus.Active,
            UtilizationRate = 1.00m,
            TotalPlannedMinutes = 3600,
            RecommendationPolicyVersion = "v1",
            StudyPlanPolicyVersion = "v1"
        });

        await db.SaveChangesAsync();

        var adminUser = CreateAdminUser(instMain.Id);
        var teacherCurrentUser = CreateTeacherUser(teacherUser.Id, instMain.Id);

        var overviewHandler = new GetInstitutionOverviewHandler(db, adminUser);
        var teacherHandler = new GetTeacherDashboardHandler(db, teacherCurrentUser, academicCatalog);

        // Warmup run
        await overviewHandler.HandleAsync();
        await teacherHandler.HandleAsync();

        // Measure GetInstitutionOverviewHandler (5 iterations)
        using var counter = new SqlCommandCounter();
        const int iterations = 5;

        counter.Reset();
        long overviewMemBefore = GC.GetTotalAllocatedBytes(precise: true);
        var overviewSw = Stopwatch.StartNew();
        InstitutionOverviewDto? overviewResult = null;
        for (int i = 0; i < iterations; i++)
        {
            overviewResult = await overviewHandler.HandleAsync();
        }
        overviewSw.Stop();
        long overviewMemAfter = GC.GetTotalAllocatedBytes(precise: true);
        int overviewQueriesPerCall = counter.Count / iterations;
        double overviewAvgMs = overviewSw.Elapsed.TotalMilliseconds / iterations;
        long overviewAvgBytes = (overviewMemAfter - overviewMemBefore) / iterations;

        // Measure GetTeacherDashboardHandler (5 iterations)
        counter.Reset();
        long teacherMemBefore = GC.GetTotalAllocatedBytes(precise: true);
        var teacherSw = Stopwatch.StartNew();
        TeacherDashboardDto? teacherResult = null;
        for (int i = 0; i < iterations; i++)
        {
            teacherResult = await teacherHandler.HandleAsync();
        }
        teacherSw.Stop();
        long teacherMemAfter = GC.GetTotalAllocatedBytes(precise: true);
        int teacherQueriesPerCall = counter.Count / iterations;
        double teacherAvgMs = teacherSw.Elapsed.TotalMilliseconds / iterations;
        long teacherAvgBytes = (teacherMemAfter - teacherMemBefore) / iterations;

        _output.WriteLine($"[MEASUREMENT] GetInstitutionOverviewHandler -> Queries/call: {overviewQueriesPerCall}, Avg Time: {overviewAvgMs:F2} ms, Avg Alloc: {overviewAvgBytes:N0} bytes");
        _output.WriteLine($"[MEASUREMENT] GetTeacherDashboardHandler -> Queries/call: {teacherQueriesPerCall}, Avg Time: {teacherAvgMs:F2} ms, Avg Alloc: {teacherAvgBytes:N0} bytes");

        overviewQueriesPerCall.Should().Be(1);
        teacherQueriesPerCall.Should().Be(1);

        // Verify data integrity against independent SQL reference queries
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync();
        }

        int refBranchCount;
        int refActiveStudents;
        int refActiveCoaches;
        int refActivePlans;
        decimal refAvgAdherence;
        decimal refAvgStudyHours;
        using (var refCmd = conn.CreateCommand())
        {
            refCmd.CommandText = @"
                SELECT
                    (SELECT COUNT(*) FROM Classrooms WHERE InstitutionId = @instId AND IsDeleted = 0),
                    (SELECT COUNT(*) FROM Students WHERE InstitutionId = @instId AND IsDeleted = 0),
                    (SELECT COUNT(*) FROM Coaches WHERE InstitutionId = @instId AND IsDeleted = 0),
                    (SELECT COUNT(*) FROM StudyPlans WHERE InstitutionId = @instId AND Status = 1 AND IsDeleted = 0),
                    ISNULL((SELECT AVG(CAST(UtilizationRate AS float)) FROM StudyPlans WHERE InstitutionId = @instId AND Status = 1 AND IsDeleted = 0), 0),
                    ISNULL((SELECT AVG(CAST(TotalPlannedMinutes AS float)) FROM StudyPlans WHERE InstitutionId = @instId AND Status = 1 AND IsDeleted = 0), 0);";
            var p = refCmd.CreateParameter();
            p.ParameterName = "@instId";
            p.Value = instMain.Id;
            refCmd.Parameters.Add(p);

            using var r = await refCmd.ExecuteReaderAsync();
            await r.ReadAsync();
            refBranchCount = r.GetInt32(0);
            refActiveStudents = r.GetInt32(1);
            refActiveCoaches = r.GetInt32(2);
            refActivePlans = r.GetInt32(3);
            refAvgAdherence = Math.Round((decimal)r.GetDouble(4) * 100m, 0);
            refAvgStudyHours = Math.Round((decimal)r.GetDouble(5) / 60m, 1);
        }

        int refTeacherDistinctClasses;
        int refTeacherDistinctStudents;
        string refTopGapTopic;
        int refTopGapWrong;
        using (var refTeacherCmd = conn.CreateCommand())
        {
            refTeacherCmd.CommandText = @"
                SELECT
                    (SELECT COUNT(DISTINCT ClassroomId) FROM ClassroomTeachers WHERE TeacherId = @teacherId AND InstitutionId = @instId AND IsDeleted = 0),
                    (SELECT COUNT(DISTINCT cs.StudentId)
                     FROM ClassroomStudents cs
                     INNER JOIN ClassroomTeachers ct ON ct.ClassroomId = cs.ClassroomId AND ct.InstitutionId = @instId AND ct.IsDeleted = 0
                     WHERE cs.InstitutionId = @instId AND cs.IsDeleted = 0 AND ct.TeacherId = @teacherId);

                SELECT TOP (1) t.Name, SUM(tr.Wrong) AS TotalWrong
                FROM StudentExamAttempts a
                INNER JOIN Exams e ON e.Id = a.ExamId AND e.IsDeleted = 0
                INNER JOIN StudentExamTopicResults tr ON tr.StudentExamAttemptId = a.Id AND tr.IsDeleted = 0
                INNER JOIN AcademicTopics t ON t.Id = tr.TopicId
                WHERE a.InstitutionId = @instId
                  AND a.IsDeleted = 0
                  AND a.Status = 1
                  AND tr.Wrong > 0
                  AND a.StudentId IN (
                      SELECT DISTINCT cs.StudentId
                      FROM ClassroomStudents cs
                      INNER JOIN ClassroomTeachers ct ON ct.ClassroomId = cs.ClassroomId AND ct.InstitutionId = @instId AND ct.IsDeleted = 0
                      WHERE cs.InstitutionId = @instId AND cs.IsDeleted = 0 AND ct.TeacherId = @teacherId
                  )
                GROUP BY a.StudentId, t.Name
                ORDER BY SUM(tr.Wrong) DESC;";
            var pInst = refTeacherCmd.CreateParameter();
            pInst.ParameterName = "@instId";
            pInst.Value = instMain.Id;
            refTeacherCmd.Parameters.Add(pInst);

            var pTeacher = refTeacherCmd.CreateParameter();
            pTeacher.ParameterName = "@teacherId";
            pTeacher.Value = teacher.Id;
            refTeacherCmd.Parameters.Add(pTeacher);

            using var r = await refTeacherCmd.ExecuteReaderAsync();
            await r.ReadAsync();
            refTeacherDistinctClasses = r.GetInt32(0);
            refTeacherDistinctStudents = r.GetInt32(1);

            await r.NextResultAsync();
            await r.ReadAsync();
            refTopGapTopic = r.GetString(0);
            refTopGapWrong = r.GetInt32(1);
        }

        overviewResult.Should().NotBeNull();
        overviewResult!.InstitutionName.Should().Be(instMain.Name);
        overviewResult.BranchCount.Should().Be(refBranchCount).And.Be(3);
        overviewResult.Kpis.Single(k => k.Label == "Aktif Öğrenci").Value.Should().Be(refActiveStudents.ToString()).And.Be("25");
        overviewResult.Kpis.Single(k => k.Label == "Aktif Koç").Value.Should().Be(refActiveCoaches.ToString()).And.Be("2");
        overviewResult.Kpis.Single(k => k.Label == "Ortalama Plan Uyumu").Value.Should().Be($"%{refAvgAdherence:0}").And.Be("%75");
        overviewResult.Kpis.Single(k => k.Label == "Ortalama Plan Uyumu").Sub.Should().Be($"{refActivePlans} aktif planda");
        overviewResult.Kpis.Single(k => k.Label == "Haftalık Çalışma Ort.").Value.Should().Be($"{refAvgStudyHours:0.0} sa");
        overviewResult.Coaches.Should().HaveCount(2);
        overviewResult.Coaches.Single(c => c.Name == "Elif Koç").Students.Should().Be(15);
        overviewResult.Coaches.Single(c => c.Name == "Elif Koç").Adherence.Should().Be(82);
        overviewResult.Coaches.Single(c => c.Name == "Murat Koç").Students.Should().Be(10);
        overviewResult.Coaches.Single(c => c.Name == "Murat Koç").Adherence.Should().Be(64);
        overviewResult.TrendData["1m"].Should().HaveCount(4);
        overviewResult.TrendData["1m"].Select(t => t.Ay).Should().ContainInOrder("Tem '26", "Ağu '26", "Eyl '26", "Eki '26");

        teacherResult.Should().NotBeNull();
        teacherResult!.TeacherName.Should().Be("Selim Yıldız");
        teacherResult.TotalAssignedClasses.Should().Be(refTeacherDistinctClasses).And.Be(2);
        // 5 students in both 12-A & 12-B + 10 in 12-A + 5 in 12-B = 20 distinct students (NOT 25!)
        teacherResult.TotalAssignedStudents.Should().Be(refTeacherDistinctStudents).And.Be(20);
        teacherResult.StudentNets.Should().HaveCount(refTeacherDistinctStudents);
        teacherResult.AcademicGaps.Should().HaveCount(8);
        // Student 0 on Topic 0 ("Türev") had 4 wrongs * 6 exams = 24 wrongs -> must be #1 gap
        teacherResult.AcademicGaps[0].Topic.Should().Be(refTopGapTopic).And.Be("Türev");
        teacherResult.AcademicGaps[0].Detail.Should().Contain($"{refTopGapWrong} yanlış");
        teacherResult.AcademicGaps[0].Severity.Should().Be("critical");
        // Each student's Matematik net increased across exams (+0.5 per exam * 4 topics = +2.0 net per exam) -> trend must be "up"
        teacherResult.StudentNets.Should().AllSatisfy(sn =>
        {
            sn.MathNet.Should().NotBeNull();
            sn.PhysicsNet.Should().NotBeNull();
            sn.Trend.Should().Be("up");
        });
    }

    [Fact]
    public async Task InstitutionOverview_And_TeacherDashboard_HandleEmptyDatasets_Cleanly()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var academicCatalog = scope.ServiceProvider.GetRequiredService<IAcademicCatalog>();

        var emptyInst = new Institution { Id = Guid.NewGuid(), Name = "Boş Kurum" };
        var emptyTeacherUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"empty-t-{Guid.NewGuid():N}@test.local",
            NormalizedUserName = $"EMPTY-T-{Guid.NewGuid():N}@TEST.LOCAL",
            Email = $"empty-t-{Guid.NewGuid():N}@test.local",
            NormalizedEmail = $"EMPTY-T-{Guid.NewGuid():N}@TEST.LOCAL",
            FirstName = "Boş",
            LastName = "Öğretmen",
            InstitutionId = emptyInst.Id,
            IsActive = true
        };
        var emptyTeacher = new Teacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = emptyInst.Id,
            UserId = emptyTeacherUser.Id,
            FirstName = "Boş",
            LastName = "Öğretmen"
        };
        db.Set<Institution>().Add(emptyInst);
        db.Set<ApplicationUser>().Add(emptyTeacherUser);
        db.Set<Teacher>().Add(emptyTeacher);
        await db.SaveChangesAsync();

        var overviewHandler = new GetInstitutionOverviewHandler(db, CreateAdminUser(emptyInst.Id));
        var overview = await overviewHandler.HandleAsync();
        overview.InstitutionName.Should().Be("Boş Kurum");
        overview.BranchCount.Should().Be(0);
        overview.Coaches.Should().BeEmpty();
        overview.TrendData["1m"].Should().BeEmpty();
        overview.Kpis.Single(k => k.Label == "Ortalama Plan Uyumu").Value.Should().Be("%0");

        var teacherHandler = new GetTeacherDashboardHandler(db, CreateTeacherUser(emptyTeacherUser.Id, emptyInst.Id), academicCatalog);
        var teacherDash = await teacherHandler.HandleAsync();
        teacherDash.TeacherName.Should().Be("Boş Öğretmen");
        teacherDash.TotalAssignedClasses.Should().Be(0);
        teacherDash.TotalAssignedStudents.Should().Be(0);
        teacherDash.AcademicGaps.Should().BeEmpty();
        teacherDash.StudentNets.Should().BeEmpty();
    }
}
