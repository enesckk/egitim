using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Security;

public static class Sprint4TestData
{
    public static Guid SubjectAId;
    public static Guid TopicAId;
    public static Guid QuestionGlobalId;
    public static Guid QuestionInstAId;
    public static Guid QuestionInstBId;
    public static Guid QuestionArchivedId;
    public static Guid ExamQuestionInstAId;
    public static Guid AttemptDraftA;

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        var subject = await db.Set<Subject>().FirstAsync(s => s.Code == "MAT");
        var topic = await db.Set<Topic>().FirstAsync(t => t.Code == "CEB");

        SubjectAId = subject.Id;
        TopicAId = topic.Id;

        // 1. Global Question (InstitutionId = null, Published)
        var globalQ = new Question
        {
            Id = Guid.NewGuid(),
            InstitutionId = null,
            SubjectId = SubjectAId,
            TopicId = TopicAId,
            QuestionType = QuestionType.MultipleChoice,
            Difficulty = DifficultyLevel.Medium,
            SourceType = QuestionSourceType.OsymPast,
            SourceYear = 2024,
            Content = "Global OSYM Question 1",
            Status = QuestionStatus.Published,
            Options = new List<QuestionOption>
            {
                new() { OptionLabel = "A", Content = "Option A", IsCorrect = true },
                new() { OptionLabel = "B", Content = "Option B", IsCorrect = false },
                new() { OptionLabel = "C", Content = "Option C", IsCorrect = false },
                new() { OptionLabel = "D", Content = "Option D", IsCorrect = false },
                new() { OptionLabel = "E", Content = "Option E", IsCorrect = false }
            }
        };
        QuestionGlobalId = globalQ.Id;

        // 2. Inst A Question (InstitutionId = Sprint2TestData.InstitutionA, Published)
        var instAQ = new Question
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            SubjectId = SubjectAId,
            TopicId = TopicAId,
            QuestionType = QuestionType.MultipleChoice,
            Difficulty = DifficultyLevel.Hard,
            SourceType = QuestionSourceType.InstitutionCustom,
            Content = "Inst A Custom Question 1",
            Status = QuestionStatus.Published,
            Options = new List<QuestionOption>
            {
                new() { OptionLabel = "A", Content = "Opt A", IsCorrect = false },
                new() { OptionLabel = "B", Content = "Opt B", IsCorrect = true },
                new() { OptionLabel = "C", Content = "Opt C", IsCorrect = false },
                new() { OptionLabel = "D", Content = "Opt D", IsCorrect = false },
                new() { OptionLabel = "E", Content = "Opt E", IsCorrect = false }
            }
        };
        QuestionInstAId = instAQ.Id;

        // 3. Inst B Question (InstitutionId = Sprint2TestData.InstitutionB, Published)
        var instBQ = new Question
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionB,
            SubjectId = SubjectAId,
            TopicId = TopicAId,
            QuestionType = QuestionType.MultipleChoice,
            Difficulty = DifficultyLevel.Easy,
            SourceType = QuestionSourceType.InstitutionCustom,
            Content = "Inst B Custom Question 1",
            Status = QuestionStatus.Published,
            Options = new List<QuestionOption>
            {
                new() { OptionLabel = "A", Content = "Opt A", IsCorrect = false },
                new() { OptionLabel = "B", Content = "Opt B", IsCorrect = false },
                new() { OptionLabel = "C", Content = "Opt C", IsCorrect = true },
                new() { OptionLabel = "D", Content = "Opt D", IsCorrect = false },
                new() { OptionLabel = "E", Content = "Opt E", IsCorrect = false }
            }
        };
        QuestionInstBId = instBQ.Id;

        // 4. Inst A Archived Question (Status = Archived)
        var archivedQ = new Question
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            SubjectId = SubjectAId,
            TopicId = TopicAId,
            QuestionType = QuestionType.MultipleChoice,
            Difficulty = DifficultyLevel.Medium,
            SourceType = QuestionSourceType.InstitutionCustom,
            Content = "Inst A Archived Question",
            Status = QuestionStatus.Archived,
            Options = new List<QuestionOption>
            {
                new() { OptionLabel = "A", Content = "Opt A", IsCorrect = true },
                new() { OptionLabel = "B", Content = "Opt B", IsCorrect = false },
                new() { OptionLabel = "C", Content = "Opt C", IsCorrect = false },
                new() { OptionLabel = "D", Content = "Opt D", IsCorrect = false },
                new() { OptionLabel = "E", Content = "Opt E", IsCorrect = false }
            }
        };
        QuestionArchivedId = archivedQ.Id;

        db.Set<Question>().AddRange(globalQ, instAQ, instBQ, archivedQ);

        // 5. Link QuestionInstA to ExamA (Institution A's exam from Sprint3TestData)
        var examQ = new ExamQuestion
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            ExamId = Sprint3TestData.ExamA,
            ExamSectionId = Sprint3TestData.SectionA,
            QuestionId = instAQ.Id,
            QuestionNumber = 1,
            Order = 1,
            CorrectOption = "B"
        };
        ExamQuestionInstAId = examQ.Id;
        db.Set<ExamQuestion>().Add(examQ);

        // 6. Draft attempt for Student A (for testing SaveAnswers on valid Draft attempt)
        var attemptDraft = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = Sprint2TestData.InstitutionA,
            ExamId = Sprint3TestData.ExamA,
            StudentId = Sprint2TestData.StudentA,
            AttemptNumber = 2,
            TakenAt = DateTimeOffset.UtcNow,
            Status = AttemptStatus.Draft,
            IsRawAvailable = false
        };
        AttemptDraftA = attemptDraft.Id;
        db.Set<StudentExamAttempt>().Add(attemptDraft);

        await db.SaveChangesAsync();
    }
}
