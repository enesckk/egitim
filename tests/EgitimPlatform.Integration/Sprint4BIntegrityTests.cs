using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint4BIntegrityTests(IntegrationTestFactory factory)
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(factory.ConnectionString).Options);

    private static string Code() => "S4B-CODE-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

    [Fact]
    public async Task DuplicateQuestionNumberInSameSection_RejectedBySql()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Q1" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Exam Title" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 40 };
        db.Add(section);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1" };
        var q2 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q2" };
        db.AddRange(q1, q2);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        // Duplicate QuestionNumber 1 in same section
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q2.Id, QuestionNumber = 1, Order = 2, CorrectOption = "B" };
        db.Add(eq2);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DuplicateQuestionIdInSameSection_RejectedBySql()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Q2" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "AYT" };
        var subject = new Subject { Code = Code(), Name = "Fizik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Kuvvet" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Exam Title" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 20 };
        db.Add(section);
        await db.SaveChangesAsync();

        var q = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q" };
        db.Add(q);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        // Same Question added twice to same section
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q.Id, QuestionNumber = 2, Order = 2, CorrectOption = "A" };
        db.Add(eq2);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DuplicateOptionLabelInSameQuestion_RejectedBySql()
    {
        using var db = Db();
        var examType = new ExamType { Code = Code(), Name = "LGS" };
        var subject = new Subject { Code = Code(), Name = "Turkce", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Paragraf" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var question = new Question
        {
            SubjectId = subject.Id,
            TopicId = topic.Id,
            Content = "Question Content",
            Options =
            [
                new QuestionOption { OptionLabel = "A", Content = "Opt 1" },
                new QuestionOption { OptionLabel = "A", Content = "Opt 2 Duplicate" }
            ]
        };

        db.Add(question);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CrossTenantExamQuestionFK_RejectedBySql()
    {
        using var db = Db();
        var instA = new Institution { Name = "Inst A" };
        var instB = new Institution { Name = "Inst B" };
        db.AddRange(instA, instB);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Kimya", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Atom" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var examA = new Exam { InstitutionId = instA.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Exam A" };
        db.Add(examA);
        await db.SaveChangesAsync();

        var sectionA = new ExamSection { InstitutionId = instA.Id, ExamId = examA.Id, SubjectId = subject.Id, QuestionCount = 10 };
        db.Add(sectionA);
        await db.SaveChangesAsync();

        var q = new Question { InstitutionId = instA.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q" };
        db.Add(q);
        await db.SaveChangesAsync();

        // Cross-tenant attempt: ExamQuestion assigned to Institution B while referencing sectionA (belongs to Inst A)
        var eqCross = new ExamQuestion
        {
            InstitutionId = instB.Id, // Foreign tenant B
            ExamId = examA.Id,
            ExamSectionId = sectionA.Id, // Section A belongs to Inst A
            QuestionId = q.Id,
            QuestionNumber = 1,
            Order = 1,
            CorrectOption = "A"
        };
        db.Add(eqCross);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
