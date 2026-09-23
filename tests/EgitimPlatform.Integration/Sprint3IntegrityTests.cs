using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint3IntegrityTests(IntegrationTestFactory factory)
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(factory.ConnectionString).Options);

    private static string Code() => "S3-CODE-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

    [Fact]
    public async Task DuplicateExamCodeInSameInstitution_RejectedBySql()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst A" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        db.Add(examType);
        await db.SaveChangesAsync();

        var code = Code();
        var exam1 = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = code, Title = "Exam 1" };
        db.Add(exam1);
        await db.SaveChangesAsync();

        var exam2 = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = code, Title = "Exam 2 Duplicate" };
        db.Add(exam2);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DuplicateAttemptNumber_RejectedBySql()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst B" };
        db.Add(inst);
        var student = new Student { InstitutionId = inst.Id, FirstName = "Test", LastName = "Student" };
        var examType = new ExamType { Code = Code(), Name = "AYT" };
        db.AddRange(student, examType);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Exam Title" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var attempt1 = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, AttemptNumber = 1 };
        db.Add(attempt1);
        await db.SaveChangesAsync();

        var attempt2 = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, AttemptNumber = 1 };
        db.Add(attempt2);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CrossTenantSectionAttemptFK_RejectedBySql()
    {
        using var db = Db();
        var instA = new Institution { Name = "Inst A" };
        var instB = new Institution { Name = "Inst B" };
        db.AddRange(instA, instB);
        await db.SaveChangesAsync();

        var studentA = new Student { InstitutionId = instA.Id, FirstName = "Student", LastName = "A" };
        var examType = new ExamType { Code = Code(), Name = "ExamType" };
        db.AddRange(studentA, examType);
        await db.SaveChangesAsync();

        var examA = new Exam { InstitutionId = instA.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Exam A" };
        db.Add(examA);
        await db.SaveChangesAsync();

        // Attempt in Institution A trying to reference foreign tenant B
        var crossTenantAttempt = new StudentExamAttempt
        {
            InstitutionId = instB.Id, // Foreign tenant
            ExamId = examA.Id,        // Exam belongs to Inst A
            StudentId = studentA.Id
        };
        db.Add(crossTenantAttempt);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task AttemptRowVersion_RejectsLostUpdate()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst RowVersion" };
        db.Add(inst);
        var student = new Student { InstitutionId = inst.Id, FirstName = "S", LastName = "T" };
        var examType = new ExamType { Code = Code(), Name = "ExamType" };
        db.AddRange(student, examType);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, AttemptNumber = 1 };
        db.Add(attempt);
        await db.SaveChangesAsync();

        using var otherDb = Db();
        var staleAttempt = await otherDb.Set<StudentExamAttempt>().SingleAsync(a => a.Id == attempt.Id);

        attempt.TotalNet = 10.0m;
        await db.SaveChangesAsync();

        staleAttempt.TotalNet = 20.0m;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => otherDb.SaveChangesAsync());
    }
}
