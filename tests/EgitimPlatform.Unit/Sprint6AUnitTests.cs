using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint6AUnitTests
{
    private static ApplicationDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SubjectPerformance_CombinesAggregateAndRawAttempts()
    {
        using var db = CreateInMemoryDb();
        var instId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, StudentNumber = "S6A-1", FirstName = "Ali", LastName = "V" };
        var exam = new Exam { Id = Guid.NewGuid(), InstitutionId = instId, ExamTypeId = Guid.NewGuid(), Code = "EX-S6A-1", Title = "S6A Exam" };
        var subjectId = Guid.NewGuid();
        var section = new ExamSection { Id = Guid.NewGuid(), InstitutionId = instId, ExamId = exam.Id, SubjectId = subjectId, QuestionCount = 10, DisplayOrder = 1 };

        db.AddRange(student, exam, section);
        await db.SaveChangesAsync();

        // 1. Finalized Aggregate Attempt (IsRawAvailable = false)
        var attemptAgg = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            ExamId = exam.Id,
            StudentId = student.Id,
            AttemptNumber = 1,
            TakenAt = DateTimeOffset.UtcNow.AddDays(-2),
            Status = AttemptStatus.Finalized,
            FinalizedAt = DateTimeOffset.UtcNow.AddDays(-2),
            IsRawAvailable = false,
            TotalCorrect = 8,
            TotalWrong = 2,
            TotalBlank = 0,
            TotalNet = 7.5m
        };
        var srAgg = new StudentExamSubjectResult
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            StudentExamAttemptId = attemptAgg.Id,
            ExamSectionId = section.Id,
            Correct = 8,
            Wrong = 2,
            Blank = 0,
            Net = 7.5m
        };

        // 2. Finalized Raw Attempt (IsRawAvailable = true)
        var attemptRaw = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            ExamId = exam.Id,
            StudentId = student.Id,
            AttemptNumber = 2,
            TakenAt = DateTimeOffset.UtcNow.AddDays(-1),
            Status = AttemptStatus.Finalized,
            FinalizedAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsRawAvailable = true,
            TotalCorrect = 6,
            TotalWrong = 2,
            TotalBlank = 2,
            TotalNet = 5.5m
        };
        var srRaw = new StudentExamSubjectResult
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            StudentExamAttemptId = attemptRaw.Id,
            ExamSectionId = section.Id,
            Correct = 6,
            Wrong = 2,
            Blank = 2,
            Net = 5.5m
        };

        db.AddRange(attemptAgg, srAgg, attemptRaw, srRaw);
        await db.SaveChangesAsync();

        var catalog = Substitute.For<IAcademicCatalog>();
        catalog.GetSubjectNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { { subjectId, "Matematik" } });

        var handler = new GetStudentAcademicIntelligenceHandler(db, catalog, Substitute.For<IExamResourceAccess>());

        // Act
        var res = await handler.GetSubjectPerformanceAsync(new AcademicIntelligenceQuery(student.Id));

        // Assert
        Assert.Single(res);
        var sub = res.Single();
        Assert.Equal(subjectId, sub.SubjectId);
        Assert.Equal("Matematik", sub.SubjectName);
        Assert.Equal(2, sub.AttemptCount);
        Assert.Equal(1, sub.RawAttemptCount);
        Assert.Equal(1, sub.AggregateAttemptCount);

        Assert.Equal(14, sub.TotalCorrect); // 8 + 6
        Assert.Equal(4, sub.TotalWrong);   // 2 + 2
        Assert.Equal(2, sub.TotalBlank);   // 0 + 2
        Assert.Equal(20, sub.TotalQuestions);
        Assert.Equal(18, sub.AttemptedCount); // 14 + 4

        // Accuracy = Correct / Attempted = 14 / 18 = 0.7778
        Assert.Equal(0.7778m, sub.Accuracy);
        // SuccessRate = Correct / TotalQuestions = 14 / 20 = 0.7000
        Assert.Equal(0.7000m, sub.SuccessRate);
        // Coverage = Attempted / TotalQuestions = 18 / 20 = 0.9000
        Assert.Equal(0.9000m, sub.Coverage);
    }

    [Fact]
    public async Task TopicPerformance_ExcludesAggregateAttempts_OnlyIncludesRaw()
    {
        using var db = CreateInMemoryDb();
        var instId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, StudentNumber = "S6A-2", FirstName = "Buse", LastName = "K" };
        var exam = new Exam { Id = Guid.NewGuid(), InstitutionId = instId, ExamTypeId = Guid.NewGuid(), Code = "EX-S6A-2", Title = "Topic Test Exam" };
        var subjectId = Guid.NewGuid();
        var topicId = Guid.NewGuid();

        var section = new ExamSection { Id = Guid.NewGuid(), InstitutionId = instId, ExamId = exam.Id, SubjectId = subjectId, QuestionCount = 5, DisplayOrder = 1 };
        db.AddRange(student, exam, section);
        await db.SaveChangesAsync();

        // Aggregate attempt (IsRawAvailable = false) -> should NOT produce topic results
        var attemptAgg = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            ExamId = exam.Id,
            StudentId = student.Id,
            AttemptNumber = 1,
            Status = AttemptStatus.Finalized,
            IsRawAvailable = false
        };

        // Raw attempt (IsRawAvailable = true) -> DOES produce topic results
        var attemptRaw = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            ExamId = exam.Id,
            StudentId = student.Id,
            AttemptNumber = 2,
            Status = AttemptStatus.Finalized,
            IsRawAvailable = true
        };
        var trRaw = new StudentExamTopicResult
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            StudentExamAttemptId = attemptRaw.Id,
            ExamSectionId = section.Id,
            TopicId = topicId,
            Correct = 4,
            Wrong = 1,
            Blank = 0,
            Net = 3.75m
        };

        db.AddRange(attemptAgg, attemptRaw, trRaw);
        await db.SaveChangesAsync();

        var catalog = Substitute.For<IAcademicCatalog>();
        catalog.GetTopicNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { { topicId, "Türev" } });
        catalog.GetSubjectNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { { subjectId, "Matematik" } });

        var handler = new GetStudentAcademicIntelligenceHandler(db, catalog, Substitute.For<IExamResourceAccess>());

        // Act
        var res = await handler.GetTopicPerformanceAsync(new AcademicIntelligenceQuery(student.Id));

        // Assert
        Assert.Single(res); // ONLY the raw attempt's topic result contributes!
        var top = res.Single();
        Assert.Equal(topicId, top.TopicId);
        Assert.Equal("Türev", top.TopicName);
        Assert.Equal(1, top.AttemptCount); // Only raw attempt counted
        Assert.Equal(4, top.TotalCorrect);
        Assert.Equal(1, top.TotalWrong);
        Assert.Equal(0.8000m, top.Accuracy); // 4 / 5
    }

    [Fact]
    public async Task DraftAttempts_AreIgnoredByAcademicIntelligence()
    {
        using var db = CreateInMemoryDb();
        var instId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, StudentNumber = "S6A-3", FirstName = "Can", LastName = "M" };
        var exam = new Exam { Id = Guid.NewGuid(), InstitutionId = instId, ExamTypeId = Guid.NewGuid(), Code = "EX-S6A-3", Title = "Draft Exam" };
        var subjectId = Guid.NewGuid();
        var section = new ExamSection { Id = Guid.NewGuid(), InstitutionId = instId, ExamId = exam.Id, SubjectId = subjectId, QuestionCount = 10, DisplayOrder = 1 };

        db.AddRange(student, exam, section);
        await db.SaveChangesAsync();

        // Draft attempt (Status = Draft)
        var draftAttempt = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            ExamId = exam.Id,
            StudentId = student.Id,
            AttemptNumber = 1,
            Status = AttemptStatus.Draft,
            IsRawAvailable = true
        };
        var srDraft = new StudentExamSubjectResult
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            StudentExamAttemptId = draftAttempt.Id,
            ExamSectionId = section.Id,
            Correct = 10,
            Wrong = 0,
            Blank = 0,
            Net = 10.0m
        };

        db.AddRange(draftAttempt, srDraft);
        await db.SaveChangesAsync();

        var catalog = Substitute.For<IAcademicCatalog>();
        var handler = new GetStudentAcademicIntelligenceHandler(db, catalog, Substitute.For<IExamResourceAccess>());

        // Act
        var res = await handler.GetSubjectPerformanceAsync(new AcademicIntelligenceQuery(student.Id));

        // Assert
        Assert.Empty(res); // Draft attempts must be completely ignored!
    }

    [Fact]
    public async Task HistoricalImportDate_RespectsExamDate_AndFiltersCorrectly()
    {
        using var db = CreateInMemoryDb();
        var instId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), InstitutionId = instId, StudentNumber = "S6A-4", FirstName = "Ece", LastName = "D" };
        var historicalExamDate = new DateTimeOffset(2025, 11, 10, 10, 0, 0, TimeSpan.Zero);
        var exam = new Exam { Id = Guid.NewGuid(), InstitutionId = instId, ExamTypeId = Guid.NewGuid(), Code = "EX-2025-11", Title = "Historical Exam", ExamDate = historicalExamDate };
        var subjectId = Guid.NewGuid();
        var section = new ExamSection { Id = Guid.NewGuid(), InstitutionId = instId, ExamId = exam.Id, SubjectId = subjectId, QuestionCount = 5, DisplayOrder = 1 };

        db.AddRange(student, exam, section);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var creator = new AggregateExamAttemptCreator(scoringService);
        var importTime = new DateTimeOffset(2026, 9, 27, 14, 0, 0, TimeSpan.Zero);

        // Act: Create attempt for historical exam
        var attempt = creator.CreateFinalizedAggregateAttempt(
            instId,
            exam.Id,
            student.Id,
            1,
            new Dictionary<Guid, ExamSection> { { section.Id, section } },
            exam,
            [(section.Id, 4, 1, 0)],
            importTime);

        // Assert: TakenAt MUST equal historical exam date, NOT import time!
        Assert.Equal(historicalExamDate, attempt.TakenAt);
        Assert.Equal(importTime, attempt.FinalizedAt);

        db.Add(attempt);
        await db.SaveChangesAsync();

        var catalog = Substitute.For<IAcademicCatalog>();
        catalog.GetSubjectNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { { subjectId, "Kimya" } });
        var handler = new GetStudentAcademicIntelligenceHandler(db, catalog, Substitute.For<IExamResourceAccess>());

        // Act 1: Query for 2025-11 window (Historical window)
        var nov2025Res = await handler.GetSubjectPerformanceAsync(new AcademicIntelligenceQuery(
            student.Id,
            From: new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero)));

        Assert.Single(nov2025Res);

        // Act 2: Query for 2026-09 window (Import window)
        var sep2026Res = await handler.GetSubjectPerformanceAsync(new AcademicIntelligenceQuery(
            student.Id,
            From: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2026, 9, 30, 23, 59, 59, TimeSpan.Zero)));

        Assert.Empty(sep2026Res); // Must NOT appear in today's window merely because it was imported today!
    }
}
