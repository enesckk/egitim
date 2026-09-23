using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Security;

public static class Sprint3TestData
{
    public static Guid ExamA, ExamForeign, SectionA, SectionForeign, AttemptA, AttemptForeign, TopicA;

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Ensure ExamType, Subject, Topic exist
        var examType = await db.Set<ExamType>().FirstOrDefaultAsync(e => e.Code == "TYT")
            ?? new ExamType { Id = Guid.NewGuid(), Code = "TYT", Name = "Temel Yeterlilik Testi" };

        var subject = await db.Set<Subject>().FirstOrDefaultAsync(s => s.Code == "MAT")
            ?? new Subject { Id = Guid.NewGuid(), ExamTypeId = examType.Id, Code = "MAT", Name = "Matematik" };

        var topic = await db.Set<Topic>().FirstOrDefaultAsync(t => t.Code == "CEB")
            ?? new Topic { Id = Guid.NewGuid(), SubjectId = subject.Id, Code = "CEB", Name = "Cebir" };

        if (db.Entry(examType).State == EntityState.Detached) db.Add(examType);
        if (db.Entry(subject).State == EntityState.Detached) db.Add(subject);
        if (db.Entry(topic).State == EntityState.Detached) db.Add(topic);
        TopicA = topic.Id;

        // Exam for Institution A
        var examA = new Exam
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            ExamTypeId = examType.Id,
            Code = "EXAM-S3-A",
            Title = "Sprint 3 Test Exam A",
            ExamDate = DateTimeOffset.UtcNow,
            Status = ExamStatus.Published,
            WrongAnswerPenalty = 4m,
            ScoreSource = ScoreSource.Institution
        };
        ExamA = examA.Id;

        var secA = new ExamSection
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            ExamId = examA.Id,
            SubjectId = subject.Id,
            DisplayOrder = 1,
            QuestionCount = 10
        };
        SectionA = secA.Id;
        examA.Sections.Add(secA);

        // Foreign Exam for Institution B
        var examB = new Exam
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionB,
            ExamTypeId = examType.Id,
            Code = "EXAM-S3-B",
            Title = "Sprint 3 Test Exam Foreign",
            ExamDate = DateTimeOffset.UtcNow,
            Status = ExamStatus.Published,
            WrongAnswerPenalty = 4m,
            ScoreSource = ScoreSource.Provider
        };
        ExamForeign = examB.Id;

        var secB = new ExamSection
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionB,
            ExamId = examB.Id,
            SubjectId = subject.Id,
            DisplayOrder = 1,
            QuestionCount = 10
        };
        SectionForeign = secB.Id;
        examB.Sections.Add(secB);

        db.AddRange(examA, examB);

        // Attempt for Student A in Institution A
        var attemptA = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            ExamId = examA.Id,
            StudentId = Sprint2TestData.StudentA,
            AttemptNumber = 1,
            TakenAt = DateTimeOffset.UtcNow,
            Status = AttemptStatus.Finalized,
            TotalCorrect = 8,
            TotalWrong = 2,
            TotalBlank = 0,
            TotalNet = 7.5m,
            FinalizedAt = DateTimeOffset.UtcNow
        };
        AttemptA = attemptA.Id;

        attemptA.SubjectResults.Add(new StudentExamSubjectResult
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            StudentExamAttemptId = attemptA.Id,
            ExamSectionId = secA.Id,
            Correct = 8,
            Wrong = 2,
            Blank = 0,
            Net = 7.5m
        });

        attemptA.TopicResults.Add(new StudentExamTopicResult
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            StudentExamAttemptId = attemptA.Id,
            ExamSectionId = secA.Id,
            TopicId = topic.Id,
            Correct = 8,
            Wrong = 2,
            Blank = 0,
            Net = 7.5m
        });

        // Foreign Attempt for Foreign Student in Institution B
        var attemptB = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionB,
            ExamId = examB.Id,
            StudentId = Sprint2TestData.StudentForeign,
            AttemptNumber = 1,
            TakenAt = DateTimeOffset.UtcNow,
            Status = AttemptStatus.Finalized,
            TotalCorrect = 10,
            TotalWrong = 0,
            TotalBlank = 0,
            TotalNet = 10.0m,
            FinalizedAt = DateTimeOffset.UtcNow
        };
        AttemptForeign = attemptB.Id;

        db.AddRange(attemptA, attemptB);
        await db.SaveChangesAsync();
    }
}
