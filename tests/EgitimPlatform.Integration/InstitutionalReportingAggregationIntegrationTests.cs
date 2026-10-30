using System.Data;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Reporting.Features.InstitutionalReports;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class InstitutionalReportingAggregationIntegrationTests
{
    private readonly IntegrationTestFactory _factory;

    public InstitutionalReportingAggregationIntegrationTests(IntegrationTestFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task GetReportDataAsync_PreventsJoinFanOut_AcrossMultiplePlansClassroomsAndAttempts_AndMatchesIndependentSql()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var instA = new Institution { Id = Guid.NewGuid(), Name = $"Bilim Akademi A {Guid.NewGuid():N}"[..24] };
        var instB = new Institution { Id = Guid.NewGuid(), Name = $"Rakip Kurum B {Guid.NewGuid():N}"[..24] };
        db.Set<Institution>().AddRange(instA, instB);

        var examType = new ExamType
        {
            Id = Guid.NewGuid(),
            Code = $"TYT_{Guid.NewGuid():N}"[..12],
            Name = "TYT Genel"
        };
        var mathSubject = new Subject
        {
            Id = Guid.NewGuid(),
            ExamTypeId = examType.Id,
            Code = $"MAT_{Guid.NewGuid():N}"[..12],
            Name = "Matematik"
        };
        var turkishSubject = new Subject
        {
            Id = Guid.NewGuid(),
            ExamTypeId = examType.Id,
            Code = $"TUR_{Guid.NewGuid():N}"[..12],
            Name = "Türkçe"
        };
        db.Set<ExamType>().Add(examType);
        db.Set<Subject>().AddRange(mathSubject, turkishSubject);

        // Classrooms in Institution A
        var class12A = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            Name = "12-A Sayısal",
            GradeLevel = 12,
            AcademicYear = "2026-2027"
        };
        var class12B = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            Name = "12-B İleri Fizik",
            GradeLevel = 12,
            AcademicYear = "2026-2027"
        };
        db.Set<Classroom>().AddRange(class12A, class12B);

        // Teachers in Institution A (2 teachers in 12-A, 1 teacher in 12-B)
        var teacherUser1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"t1-{Guid.NewGuid():N}@test.local",
            NormalizedUserName = $"T1-{Guid.NewGuid():N}@TEST.LOCAL",
            Email = $"t1-{Guid.NewGuid():N}@test.local",
            NormalizedEmail = $"T1-{Guid.NewGuid():N}@TEST.LOCAL",
            FirstName = "Kemal",
            LastName = "Hoca",
            InstitutionId = instA.Id,
            IsActive = true
        };
        var teacherUser2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"t2-{Guid.NewGuid():N}@test.local",
            NormalizedUserName = $"T2-{Guid.NewGuid():N}@TEST.LOCAL",
            Email = $"t2-{Guid.NewGuid():N}@test.local",
            NormalizedEmail = $"T2-{Guid.NewGuid():N}@TEST.LOCAL",
            FirstName = "Zeynep",
            LastName = "Hoca",
            InstitutionId = instA.Id,
            IsActive = true
        };
        db.Set<ApplicationUser>().AddRange(teacherUser1, teacherUser2);

        var teacher1 = new Teacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            UserId = teacherUser1.Id,
            FirstName = "Kemal",
            LastName = "Hoca"
        };
        var teacher2 = new Teacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            UserId = teacherUser2.Id,
            FirstName = "Zeynep",
            LastName = "Hoca"
        };
        db.Set<Teacher>().AddRange(teacher1, teacher2);
        db.Set<ClassroomTeacher>().AddRange(
            new ClassroomTeacher { Id = Guid.NewGuid(), InstitutionId = instA.Id, ClassroomId = class12A.Id, TeacherId = teacher1.Id },
            new ClassroomTeacher { Id = Guid.NewGuid(), InstitutionId = instA.Id, ClassroomId = class12A.Id, TeacherId = teacher2.Id },
            new ClassroomTeacher { Id = Guid.NewGuid(), InstitutionId = instA.Id, ClassroomId = class12B.Id, TeacherId = teacher1.Id }
        );

        // Students in Institution A:
        // - Student 1 is enrolled in BOTH 12-A and 12-B (multi-classroom enrollment)
        // - Student 2 is enrolled in 12-A only
        // - StudentDeleted is soft-deleted (must never be counted)
        var student1 = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            UserId = Guid.NewGuid(),
            FirstName = "Ahmet",
            LastName = "Yılmaz",
            StudentNumber = "1001"
        };
        var student2 = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            UserId = Guid.NewGuid(),
            FirstName = "Ayşe",
            LastName = "Kaya",
            StudentNumber = "1002"
        };
        var deletedStudent = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            UserId = Guid.NewGuid(),
            FirstName = "Silinen",
            LastName = "Öğrenci",
            StudentNumber = "9999",
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow
        };
        db.Set<Student>().AddRange(student1, student2, deletedStudent);

        db.Set<ClassroomStudent>().AddRange(
            new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instA.Id, ClassroomId = class12A.Id, StudentId = student1.Id },
            new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instA.Id, ClassroomId = class12B.Id, StudentId = student1.Id },
            new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instA.Id, ClassroomId = class12A.Id, StudentId = student2.Id },
            new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instA.Id, ClassroomId = class12A.Id, StudentId = deletedStudent.Id }
        );

        // Multiple StudyPlans per student:
        // Student 1 has 1 Active (0.80), 1 Completed (0.10), 1 Cancelled (0.20), and 1 Draft (0.05) plan
        // Student 2 has 1 Active (0.60) and 1 Completed (0.95) plan
        var now = DateTimeOffset.UtcNow;
        db.Set<StudyPlan>().AddRange(
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentId = student1.Id,
                Title = "S1 Active Plan",
                StartDate = now.AddDays(-7),
                EndDate = now.AddDays(7),
                Status = StudyPlanStatus.Active,
                UtilizationRate = 0.80m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            },
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentId = student1.Id,
                Title = "S1 Completed Historical Plan",
                StartDate = now.AddDays(-30),
                EndDate = now.AddDays(-15),
                Status = StudyPlanStatus.Completed,
                UtilizationRate = 0.10m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            },
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentId = student1.Id,
                Title = "S1 Cancelled Plan",
                StartDate = now.AddDays(-20),
                EndDate = now.AddDays(-10),
                Status = StudyPlanStatus.Cancelled,
                UtilizationRate = 0.20m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            },
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentId = student1.Id,
                Title = "S1 Draft Plan",
                StartDate = now,
                EndDate = now.AddDays(14),
                Status = StudyPlanStatus.Draft,
                UtilizationRate = 0.05m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            },
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentId = student2.Id,
                Title = "S2 Active Plan",
                StartDate = now.AddDays(-7),
                EndDate = now.AddDays(7),
                Status = StudyPlanStatus.Active,
                UtilizationRate = 0.60m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            },
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentId = student2.Id,
                Title = "S2 Completed Plan",
                StartDate = now.AddDays(-40),
                EndDate = now.AddDays(-25),
                Status = StudyPlanStatus.Completed,
                UtilizationRate = 0.95m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            }
        );

        // Exams in Institution A
        var exam1 = new Exam
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamTypeId = examType.Id,
            Code = $"EX1-{Guid.NewGuid():N}"[..12],
            Title = "Kurumsal Deneme 1",
            ExamDate = now.AddDays(-10),
            Status = ExamStatus.Published
        };
        var exam2 = new Exam
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamTypeId = examType.Id,
            Code = $"EX2-{Guid.NewGuid():N}"[..12],
            Title = "Kurumsal Deneme 2",
            ExamDate = now.AddDays(-2),
            Status = ExamStatus.Published
        };
        db.Set<Exam>().AddRange(exam1, exam2);

        var exam1MathSec = new ExamSection
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamId = exam1.Id,
            SubjectId = mathSubject.Id,
            DisplayOrder = 1,
            QuestionCount = 40
        };
        var exam1TurkSec = new ExamSection
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamId = exam1.Id,
            SubjectId = turkishSubject.Id,
            DisplayOrder = 2,
            QuestionCount = 40
        };
        var exam2MathSec = new ExamSection
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamId = exam2.Id,
            SubjectId = mathSubject.Id,
            DisplayOrder = 1,
            QuestionCount = 40
        };
        db.Set<ExamSection>().AddRange(exam1MathSec, exam1TurkSec, exam2MathSec);

        // Multiple Exam Attempts:
        // Student 1 (enrolled in 2 classrooms!) has 3 attempts: nets 90, 100, 110
        // Student 2 (enrolled in 1 classroom) has 1 attempt: net 60
        var attempt1S1 = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamId = exam1.Id,
            StudentId = student1.Id,
            AttemptNumber = 1,
            Status = AttemptStatus.Finalized,
            TotalCorrect = 94,
            TotalWrong = 16,
            TotalBlank = 10,
            TotalNet = 90.00m,
            ReportedScore = 410.00m,
            CreatedAt = now.AddDays(-10)
        };
        var attempt2S1 = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamId = exam1.Id,
            StudentId = student1.Id,
            AttemptNumber = 2,
            Status = AttemptStatus.Finalized,
            TotalCorrect = 103,
            TotalWrong = 12,
            TotalBlank = 5,
            TotalNet = 100.00m,
            ReportedScore = 440.00m,
            CreatedAt = now.AddDays(-5)
        };
        var attempt3S1 = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamId = exam2.Id,
            StudentId = student1.Id,
            AttemptNumber = 1,
            Status = AttemptStatus.Finalized,
            TotalCorrect = 111,
            TotalWrong = 4,
            TotalBlank = 5,
            TotalNet = 110.00m,
            ReportedScore = 470.00m,
            CreatedAt = now.AddDays(-2)
        };
        var attempt4S2 = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA.Id,
            ExamId = exam1.Id,
            StudentId = student2.Id,
            AttemptNumber = 1,
            Status = AttemptStatus.Finalized,
            TotalCorrect = 64,
            TotalWrong = 16,
            TotalBlank = 40,
            TotalNet = 60.00m,
            ReportedScore = 320.00m,
            CreatedAt = now.AddDays(-1)
        };
        db.Set<StudentExamAttempt>().AddRange(attempt1S1, attempt2S1, attempt3S1, attempt4S2);

        db.Set<StudentExamSubjectResult>().AddRange(
            new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentExamAttemptId = attempt1S1.Id,
                ExamSectionId = exam1MathSec.Id,
                Correct = 31,
                Wrong = 4,
                Blank = 5,
                Net = 30.00m
            },
            new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentExamAttemptId = attempt2S1.Id,
                ExamSectionId = exam1MathSec.Id,
                Correct = 36,
                Wrong = 4,
                Blank = 0,
                Net = 35.00m
            },
            new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentExamAttemptId = attempt3S1.Id,
                ExamSectionId = exam2MathSec.Id,
                Correct = 40,
                Wrong = 0,
                Blank = 0,
                Net = 40.00m
            },
            new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentExamAttemptId = attempt4S2.Id,
                ExamSectionId = exam1MathSec.Id,
                Correct = 20,
                Wrong = 8,
                Blank = 12,
                Net = 18.00m
            },
            new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentExamAttemptId = attempt1S1.Id,
                ExamSectionId = exam1TurkSec.Id,
                Correct = 32,
                Wrong = 8,
                Blank = 0,
                Net = 30.00m
            },
            new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = instA.Id,
                StudentExamAttemptId = attempt4S2.Id,
                ExamSectionId = exam1TurkSec.Id,
                Correct = 22,
                Wrong = 8,
                Blank = 10,
                Net = 20.00m
            }
        );

        // Competing Institution B records (must be completely isolated from Institution A)
        var studentB = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instB.Id,
            UserId = Guid.NewGuid(),
            FirstName = "Burak",
            LastName = "BaşkaKurum",
            StudentNumber = "2001"
        };
        var examB = new Exam
        {
            Id = Guid.NewGuid(),
            InstitutionId = instB.Id,
            ExamTypeId = examType.Id,
            Code = $"EXB-{Guid.NewGuid():N}"[..12],
            Title = "Kurum B Denemesi",
            ExamDate = now.AddDays(-1),
            Status = ExamStatus.Published
        };
        db.Set<Student>().Add(studentB);
        db.Set<Exam>().Add(examB);
        db.Set<StudyPlan>().Add(new StudyPlan
        {
            Id = Guid.NewGuid(),
            InstitutionId = instB.Id,
            StudentId = studentB.Id,
            Title = "InstB Active Plan",
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(5),
            Status = StudyPlanStatus.Active,
            UtilizationRate = 1.00m,
            RecommendationPolicyVersion = "v1",
            StudyPlanPolicyVersion = "v1"
        });
        db.Set<StudentExamAttempt>().Add(new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instB.Id,
            ExamId = examB.Id,
            StudentId = studentB.Id,
            AttemptNumber = 1,
            Status = AttemptStatus.Finalized,
            TotalCorrect = 120,
            TotalWrong = 0,
            TotalBlank = 0,
            TotalNet = 120.00m,
            ReportedScore = 500.00m,
            CreatedAt = now.AddDays(-1)
        });

        await db.SaveChangesAsync();

        // Preserve exact seeded timestamps for attempt CreatedAt filtering
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE StudentExamAttempts SET CreatedAt = {now.AddDays(-10)} WHERE Id = {attempt1S1.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE StudentExamAttempts SET CreatedAt = {now.AddDays(-5)} WHERE Id = {attempt2S1.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE StudentExamAttempts SET CreatedAt = {now.AddDays(-2)} WHERE Id = {attempt3S1.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE StudentExamAttempts SET CreatedAt = {now.AddDays(-1)} WHERE Id = {attempt4S2.Id}");

        var handlerA = new InstitutionalReportHandler(db, CreateAdminUser(instA.Id));
        var reportA = await handlerA.GetReportDataAsync(new ReportFilterRequest());

        // Compute independent SQL reference values at the canonical grain directly against MSSQL
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        int sqlDistinctStudents;
        int sqlDistinctAttempts;
        int sqlDistinctExams;
        decimal sqlAvgNet;
        decimal sqlAvgAdherence;
        decimal sqlClass12AAdherence;
        decimal sqlClass12ANet;
        decimal sqlMathAvgNet;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT COUNT(Id) FROM Students WHERE InstitutionId = @instId AND IsDeleted = 0;
                SELECT COUNT(Id), COUNT(DISTINCT ExamId), ROUND(AVG(TotalNet), 2) FROM StudentExamAttempts WHERE InstitutionId = @instId AND IsDeleted = 0;
                SELECT ROUND(AVG(UtilizationRate * 100.0), 1) FROM StudyPlans WHERE InstitutionId = @instId AND Status = 1 AND IsDeleted = 0;
                SELECT ROUND(AVG(p.UtilizationRate * 100.0), 1)
                FROM StudyPlans p
                WHERE p.InstitutionId = @instId AND p.Status = 1 AND p.IsDeleted = 0
                  AND p.StudentId IN (SELECT cs.StudentId FROM ClassroomStudents cs WHERE cs.ClassroomId = @class12AId AND cs.IsDeleted = 0);
                SELECT ROUND(AVG(a.TotalNet), 2)
                FROM StudentExamAttempts a
                WHERE a.InstitutionId = @instId AND a.IsDeleted = 0
                  AND a.StudentId IN (SELECT cs.StudentId FROM ClassroomStudents cs WHERE cs.ClassroomId = @class12AId AND cs.IsDeleted = 0);
                SELECT ROUND(AVG(sr.Net), 2)
                FROM StudentExamSubjectResults sr
                JOIN ExamSections es ON es.Id = sr.ExamSectionId
                WHERE sr.InstitutionId = @instId AND sr.IsDeleted = 0 AND es.SubjectId = @mathSubId;";

            var pInst = cmd.CreateParameter();
            pInst.ParameterName = "@instId";
            pInst.Value = instA.Id;
            cmd.Parameters.Add(pInst);

            var pClass = cmd.CreateParameter();
            pClass.ParameterName = "@class12AId";
            pClass.Value = class12A.Id;
            cmd.Parameters.Add(pClass);

            var pMath = cmd.CreateParameter();
            pMath.ParameterName = "@mathSubId";
            pMath.Value = mathSubject.Id;
            cmd.Parameters.Add(pMath);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            sqlDistinctStudents = reader.GetInt32(0);

            await reader.NextResultAsync();
            await reader.ReadAsync();
            sqlDistinctAttempts = reader.GetInt32(0);
            sqlDistinctExams = reader.GetInt32(1);
            sqlAvgNet = reader.GetDecimal(2);

            await reader.NextResultAsync();
            await reader.ReadAsync();
            sqlAvgAdherence = Convert.ToDecimal(reader.GetValue(0));

            await reader.NextResultAsync();
            await reader.ReadAsync();
            sqlClass12AAdherence = Convert.ToDecimal(reader.GetValue(0));

            await reader.NextResultAsync();
            await reader.ReadAsync();
            sqlClass12ANet = reader.GetDecimal(0);

            await reader.NextResultAsync();
            await reader.ReadAsync();
            sqlMathAvgNet = reader.GetDecimal(0);
        }

        // 1. Institution-wide KPI assertions (matching both independent SQL and exact mathematical expectations)
        reportA.TotalStudents.Should().Be(sqlDistinctStudents).And.Be(2, "Student 1 in two classrooms must not be counted twice");
        reportA.TotalAttempts.Should().Be(sqlDistinctAttempts).And.Be(4, "Student 1's 3 attempts must not be duplicated into 6 by multi-classroom membership");
        reportA.TotalExams.Should().Be(sqlDistinctExams).And.Be(2);
        reportA.OverallAverageNet.Should().Be(sqlAvgNet).And.Be(90.00m, "(90 + 100 + 110 + 60) / 4 = 90.00, not 94.29 from multi-classroom duplication");
        reportA.OverallAverageAdherence.Should().Be(sqlAvgAdherence).And.Be(70.0m, "(80% + 60%) / 2 = 70.0%, unaffected by completed/cancelled plans or exam attempt fan-out");

        // 2. BranchComparisons assertions (no StudyPlans x StudentExamAttempts x ClassroomTeachers Cartesian fan-out)
        reportA.BranchComparisons.Should().HaveCount(2);
        var branch12A = reportA.BranchComparisons.Single(b => b.ClassroomId == class12A.Id);
        branch12A.StudentCount.Should().Be(2);
        branch12A.CoachCount.Should().Be(2);
        branch12A.AverageAdherence.Should().Be(sqlClass12AAdherence).And.Be(70.0m, "12-A adherence is (80 + 60)/2 = 70.0%, not 75.0% from S1 having 3 exam attempts");
        branch12A.AverageNet.Should().Be(sqlClass12ANet).And.Be(90.00m);

        var branch12B = reportA.BranchComparisons.Single(b => b.ClassroomId == class12B.Id);
        branch12B.StudentCount.Should().Be(1);
        branch12B.CoachCount.Should().Be(1);
        branch12B.AverageAdherence.Should().Be(80.0m);
        branch12B.AverageNet.Should().Be(100.00m, "(90 + 100 + 110) / 3 = 100.00");

        // 3. AdherenceDistribution assertions (only active plans counted: 1 High [0.80], 1 Mid [0.60], 0 Low)
        reportA.AdherenceDistribution.Sum(d => d.StudentCount).Should().Be(2);
        reportA.AdherenceDistribution.Single(d => d.Range.Contains("80")).StudentCount.Should().Be(1);
        reportA.AdherenceDistribution.Single(d => d.Range.Contains("50–%79")).StudentCount.Should().Be(1);
        reportA.AdherenceDistribution.Single(d => d.Range.Contains("<%50")).StudentCount.Should().Be(0);

        // 4. SubjectAverages assertions (no multi-classroom duplication on StudentExamSubjectResults)
        var mathRow = reportA.SubjectAverages.Single(s => s.SubjectName == "Matematik");
        mathRow.AverageNet.Should().Be(sqlMathAvgNet).And.Be(30.75m, "(30 + 35 + 40 + 18) / 4 = 30.75");
        mathRow.AverageCorrect.Should().Be(31.8m, "(31 + 36 + 40 + 20) / 4 = 31.75 -> 31.8");
        mathRow.AverageWrong.Should().Be(4.0m, "(4 + 4 + 0 + 8) / 4 = 4.0");

        var turkRow = reportA.SubjectAverages.Single(s => s.SubjectName == "Türkçe");
        turkRow.AverageNet.Should().Be(25.00m, "(30 + 20) / 2 = 25.00");

        // 5. ExamPerformances assertions (strictly 1 row per StudentExamAttempt, unique AttemptIds)
        reportA.ExamPerformances.Should().HaveCount(4);
        reportA.ExamPerformances.Select(r => r.AttemptId).Should().OnlyHaveUniqueItems();
        reportA.ExamPerformances.Where(r => r.StudentName == "Ahmet Yılmaz")
            .Should().AllSatisfy(r => r.ClassroomName.Should().Be("12-A Sayısal, 12-B İleri Fizik"));
    }

    [Fact]
    public async Task GetReportDataAsync_FilteredAndEmptyScenarios_RespectFiltersAndInstitutionIsolation()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var instFiltered = new Institution { Id = Guid.NewGuid(), Name = $"Filtre Kurumu {Guid.NewGuid():N}"[..22] };
        var instEmpty = new Institution { Id = Guid.NewGuid(), Name = $"Bos Kurum {Guid.NewGuid():N}"[..20] };
        db.Set<Institution>().AddRange(instFiltered, instEmpty);

        var examType = new ExamType
        {
            Id = Guid.NewGuid(),
            Code = $"AYT_{Guid.NewGuid():N}"[..12],
            Name = "AYT Sayısal"
        };
        var physicsSubject = new Subject
        {
            Id = Guid.NewGuid(),
            ExamTypeId = examType.Id,
            Code = $"FIZ_{Guid.NewGuid():N}"[..12],
            Name = "Fizik"
        };
        db.Set<ExamType>().Add(examType);
        db.Set<Subject>().Add(physicsSubject);

        var classA = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            Name = "12-Fen-A",
            GradeLevel = 12,
            AcademicYear = "2026-2027"
        };
        var classB = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            Name = "12-Fen-B",
            GradeLevel = 12,
            AcademicYear = "2026-2027"
        };
        db.Set<Classroom>().AddRange(classA, classB);

        // 3 students: S_A in classA, S_B in classB, S_Unassigned in no classroom
        var studentA = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            UserId = Guid.NewGuid(),
            FirstName = "Can",
            LastName = "A",
            StudentNumber = "3001"
        };
        var studentB = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            UserId = Guid.NewGuid(),
            FirstName = "Deniz",
            LastName = "B",
            StudentNumber = "3002"
        };
        var studentUnassigned = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            UserId = Guid.NewGuid(),
            FirstName = "Efe",
            LastName = "Genel",
            StudentNumber = "3003"
        };
        db.Set<Student>().AddRange(studentA, studentB, studentUnassigned);

        db.Set<ClassroomStudent>().AddRange(
            new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instFiltered.Id, ClassroomId = classA.Id, StudentId = studentA.Id },
            new ClassroomStudent { Id = Guid.NewGuid(), InstitutionId = instFiltered.Id, ClassroomId = classB.Id, StudentId = studentB.Id }
        );

        var now = DateTimeOffset.UtcNow;
        db.Set<StudyPlan>().AddRange(
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instFiltered.Id,
                StudentId = studentA.Id,
                Title = "Plan A",
                StartDate = now.AddDays(-5),
                EndDate = now.AddDays(5),
                Status = StudyPlanStatus.Active,
                UtilizationRate = 0.90m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            },
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instFiltered.Id,
                StudentId = studentB.Id,
                Title = "Plan B",
                StartDate = now.AddDays(-5),
                EndDate = now.AddDays(5),
                Status = StudyPlanStatus.Active,
                UtilizationRate = 0.30m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            },
            new StudyPlan
            {
                Id = Guid.NewGuid(),
                InstitutionId = instFiltered.Id,
                StudentId = studentUnassigned.Id,
                Title = "Plan Unassigned",
                StartDate = now.AddDays(-5),
                EndDate = now.AddDays(5),
                Status = StudyPlanStatus.Active,
                UtilizationRate = 0.60m,
                RecommendationPolicyVersion = "v1",
                StudyPlanPolicyVersion = "v1"
            }
        );

        var examOld = new Exam
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            ExamTypeId = examType.Id,
            Code = $"EXO-{Guid.NewGuid():N}"[..12],
            Title = "Eski Sınav",
            ExamDate = now.AddDays(-20),
            Status = ExamStatus.Published
        };
        var examNew = new Exam
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            ExamTypeId = examType.Id,
            Code = $"EXN-{Guid.NewGuid():N}"[..12],
            Title = "Yeni Sınav",
            ExamDate = now.AddDays(-1),
            Status = ExamStatus.Published
        };
        db.Set<Exam>().AddRange(examOld, examNew);

        var secOld = new ExamSection { Id = Guid.NewGuid(), InstitutionId = instFiltered.Id, ExamId = examOld.Id, SubjectId = physicsSubject.Id, DisplayOrder = 1, QuestionCount = 14 };
        var secNew = new ExamSection { Id = Guid.NewGuid(), InstitutionId = instFiltered.Id, ExamId = examNew.Id, SubjectId = physicsSubject.Id, DisplayOrder = 1, QuestionCount = 14 };
        db.Set<ExamSection>().AddRange(secOld, secNew);

        var attOldA = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            ExamId = examOld.Id,
            StudentId = studentA.Id,
            AttemptNumber = 1,
            Status = AttemptStatus.Finalized,
            TotalNet = 50.00m,
            ReportedScore = 300.00m
        };
        var attNewB = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instFiltered.Id,
            ExamId = examNew.Id,
            StudentId = studentB.Id,
            AttemptNumber = 1,
            Status = AttemptStatus.Finalized,
            TotalNet = 80.00m,
            ReportedScore = 400.00m
        };
        db.Set<StudentExamAttempt>().AddRange(attOldA, attNewB);
        db.Set<StudentExamSubjectResult>().AddRange(
            new StudentExamSubjectResult { Id = Guid.NewGuid(), InstitutionId = instFiltered.Id, StudentExamAttemptId = attOldA.Id, ExamSectionId = secOld.Id, Correct = 10, Wrong = 0, Blank = 4, Net = 10.00m },
            new StudentExamSubjectResult { Id = Guid.NewGuid(), InstitutionId = instFiltered.Id, StudentExamAttemptId = attNewB.Id, ExamSectionId = secNew.Id, Correct = 14, Wrong = 0, Blank = 0, Net = 14.00m }
        );

        await db.SaveChangesAsync();

        var oldDate = now.AddDays(-20);
        var newDate = now.AddDays(-1);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE StudentExamAttempts SET CreatedAt = {oldDate} WHERE Id = {attOldA.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE StudentExamAttempts SET CreatedAt = {newDate} WHERE Id = {attNewB.Id}");

        var handler = new InstitutionalReportHandler(db, CreateAdminUser(instFiltered.Id));

        // 1. Unfiltered includes all 3 distinct students (including unassigned studentUnassigned) and all 3 active plans
        var unfiltered = await handler.GetReportDataAsync(new ReportFilterRequest());
        unfiltered.TotalStudents.Should().Be(3);
        unfiltered.OverallAverageAdherence.Should().Be(60.0m, "(90 + 30 + 60) / 3 = 60.0%");
        unfiltered.TotalAttempts.Should().Be(2);
        unfiltered.OverallAverageNet.Should().Be(65.00m);

        // 2. Classroom Filter (classB only -> studentB)
        var byClassB = await handler.GetReportDataAsync(new ReportFilterRequest(ClassroomId: classB.Id));
        byClassB.TotalStudents.Should().Be(1);
        byClassB.TotalAttempts.Should().Be(1);
        byClassB.TotalExams.Should().Be(1);
        byClassB.OverallAverageNet.Should().Be(80.00m);
        byClassB.OverallAverageAdherence.Should().Be(30.0m);
        byClassB.SubjectAverages.Single(s => s.SubjectName == "Fizik").AverageNet.Should().Be(14.00m);

        // 3. Exam Filter (examOld only -> attOldA)
        var byExamOld = await handler.GetReportDataAsync(new ReportFilterRequest(ExamId: examOld.Id));
        byExamOld.TotalAttempts.Should().Be(1);
        byExamOld.TotalExams.Should().Be(1);
        byExamOld.OverallAverageNet.Should().Be(50.00m);
        byExamOld.SubjectAverages.Single(s => s.SubjectName == "Fizik").AverageNet.Should().Be(10.00m);

        // 4. Date Range Filter (only recent 5 days -> attNewB)
        var byDateRange = await handler.GetReportDataAsync(new ReportFilterRequest(StartDate: now.AddDays(-5), EndDate: now));
        byDateRange.TotalAttempts.Should().Be(1);
        byDateRange.OverallAverageNet.Should().Be(80.00m);

        // 5. Empty Institution Report
        var emptyHandler = new InstitutionalReportHandler(db, CreateAdminUser(instEmpty.Id));
        var emptyReport = await emptyHandler.GetReportDataAsync(new ReportFilterRequest());
        emptyReport.TotalStudents.Should().Be(0);
        emptyReport.TotalExams.Should().Be(0);
        emptyReport.TotalAttempts.Should().Be(0);
        emptyReport.OverallAverageNet.Should().Be(0m);
        emptyReport.OverallAverageAdherence.Should().Be(0m);
        emptyReport.BranchComparisons.Should().BeEmpty();
        emptyReport.SubjectAverages.Should().BeEmpty();
        emptyReport.ExamPerformances.Should().BeEmpty();
        emptyReport.AdherenceDistribution.Sum(d => d.StudentCount).Should().Be(0);
    }
}
