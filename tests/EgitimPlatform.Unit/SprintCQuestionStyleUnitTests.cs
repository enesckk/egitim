using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.AI.Features;
using EgitimPlatform.Modules.AI.Services;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;
using EgitimPlatform.Modules.Questions.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class SprintCQuestionStyleUnitTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void RecommendationNodeType_PreservesBackwardCompatibleEnumValues_AndAddsQuestionStyleAs3()
    {
        ((int)RecommendationNodeType.Subject).Should().Be(0);
        ((int)RecommendationNodeType.Topic).Should().Be(1);
        ((int)RecommendationNodeType.LearningOutcome).Should().Be(2);
        ((int)RecommendationNodeType.QuestionStyle).Should().Be(3);
    }

    [Fact]
    public void CalculatePriorityScore_QuestionStyleSpecificityBonus_IsBetweenTopicAndSubject()
    {
        var loScore = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Weak, TrendDirection.Stable, 0.50m, RecommendationNodeType.LearningOutcome);
        var topicScore = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Weak, TrendDirection.Stable, 0.50m, RecommendationNodeType.Topic);
        var styleScore = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Weak, TrendDirection.Stable, 0.50m, RecommendationNodeType.QuestionStyle);
        var subjectScore = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Weak, TrendDirection.Stable, 0.50m, RecommendationNodeType.Subject);

        loScore.Should().BeGreaterThan(topicScore);
        topicScore.Should().Be(styleScore);
        styleScore.Should().BeGreaterThan(subjectScore);
        (styleScore - subjectScore).Should().Be(
            AcademicRecommendationPolicy.SpecificityBonusQuestionStyle - AcademicRecommendationPolicy.SpecificityBonusSubject);
    }

    [Fact]
    public void SortAndLimitRecommendations_OrdersNodeType_LearningOutcome_Topic_QuestionStyle_Subject()
    {
        // All four candidates share ClassificationState = Weak and PriorityScore = 70
        var subjectCandidate = new AcademicRecommendationDto(
            AcademicRecommendationType.StrengthenWeak,
            RecommendationNodeType.Subject, Guid.NewGuid(), "A_SUB", "Subject",
            Guid.NewGuid(), "Subject", null, null, null, null, null, null,
            AcademicClassificationState.Weak, ClassificationBasis.OverallHistory,
            0.50m, 0.50m, 1m, TrendDirection.Stable, 0m, 3, 10, 70, "v1", "v1");

        var styleCandidate = new AcademicRecommendationDto(
            AcademicRecommendationType.StrengthenWeak,
            RecommendationNodeType.QuestionStyle, Guid.NewGuid(), "A_STYLE", "Style",
            null, null, null, null, null, null, null, null,
            AcademicClassificationState.Weak, ClassificationBasis.OverallHistory,
            0.50m, 0.50m, 1m, TrendDirection.Stable, 0m, 3, 10, 70, "v1", "v1");

        var topicCandidate = new AcademicRecommendationDto(
            AcademicRecommendationType.StrengthenWeak,
            RecommendationNodeType.Topic, Guid.NewGuid(), "A_TOP", "Topic",
            Guid.NewGuid(), "Subject 2", Guid.NewGuid(), "Topic", null, null, null, null,
            AcademicClassificationState.Weak, ClassificationBasis.OverallHistory,
            0.50m, 0.50m, 1m, TrendDirection.Stable, 0m, 3, 10, 70, "v1", "v1");

        var loCandidate = new AcademicRecommendationDto(
            AcademicRecommendationType.StrengthenWeak,
            RecommendationNodeType.LearningOutcome, Guid.NewGuid(), "A_LO", "LO",
            Guid.NewGuid(), "Subject 3", Guid.NewGuid(), "Topic 3", Guid.NewGuid(), "SubTopic 3", Guid.NewGuid(), "LO",
            AcademicClassificationState.Weak, ClassificationBasis.OverallHistory,
            0.50m, 0.50m, 1m, TrendDirection.Stable, 0m, 3, 10, 70, "v1", "v1");

        var recs = AcademicRecommendationCalculator.SortAndLimitRecommendations(
            [subjectCandidate, styleCandidate, topicCandidate, loCandidate], limit: 10);

        recs.Should().HaveCount(4);
        recs.Select(r => r.NodeType).Should().ContainInOrder(
            RecommendationNodeType.LearningOutcome,
            RecommendationNodeType.Topic,
            RecommendationNodeType.QuestionStyle,
            RecommendationNodeType.Subject);
    }

    [Fact]
    public async Task GetRecommendations_MultiTaggedQuestions_ExcludesStrongAndInsufficientData_NoDuplicateStyles_PreservesSuppression()
    {
        using var db = Db();
        var inst = new Institution { Name = "Sprint C Inst" };
        db.Add(inst);

        var examType = new ExamType { Code = "TYT", Name = "TYT" };
        var subject = new Subject { Code = "MAT", Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = "PROB", Name = "Problemler", SubjectId = subject.Id };
        var subTopic = new SubTopic { Code = "PROB-1", Name = "Sayı Problemleri", TopicId = topic.Id };
        var lo = new LearningOutcome { Code = "MAT.9.1", Name = "Denklem Kurma", SubTopicId = subTopic.Id };

        var styleNewGen = new QuestionStyle { Id = Guid.NewGuid(), Code = "NEW_GENERATION", Name = "Yeni Nesil", DisplayOrder = 1, IsActive = true };
        var styleParagraph = new QuestionStyle { Id = Guid.NewGuid(), Code = "LONG_TEXT_PARAGRAPH", Name = "Uzun Metin / Paragraf", DisplayOrder = 2, IsActive = true };
        var styleStrong = new QuestionStyle { Id = Guid.NewGuid(), Code = "CONCEPT_KNOWLEDGE", Name = "Kavram Bilgisi", DisplayOrder = 3, IsActive = true };
        var styleInsuff = new QuestionStyle { Id = Guid.NewGuid(), Code = "GRAPH_TABLE", Name = "Grafik ve Tablo Yorumlama", DisplayOrder = 4, IsActive = true };

        db.AddRange(examType, subject, topic, subTopic, lo, styleNewGen, styleParagraph, styleStrong, styleInsuff);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 2 finalized attempts, each has:
        // - 3 multi-tagged wrong questions (NEW_GENERATION + LONG_TEXT_PARAGRAPH) -> 6 questions total, Critical
        // - 3 correct questions for CONCEPT_KNOWLEDGE -> 6 questions total, Strong
        // - 1 wrong question for GRAPH_TABLE -> 2 questions total (< 5), InsufficientData
        for (int a = 1; a <= 2; a++)
        {
            var exam = new Exam { InstitutionId = inst.Id, Title = $"Deneme {a}", ExamTypeId = examType.Id, ExamDate = now.AddDays(-5 + a) };
            db.Add(exam);
            var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 7 };
            db.Add(section);
            var attempt = new StudentExamAttempt
            {
                InstitutionId = inst.Id,
                ExamId = exam.Id,
                StudentId = studentId,
                TakenAt = exam.ExamDate,
                AttemptNumber = a,
                Status = AttemptStatus.Finalized,
                IsRawAvailable = true
            };
            db.Add(attempt);
            await db.SaveChangesAsync();

            for (int qIdx = 1; qIdx <= 3; qIdx++)
            {
                var qMulti = new Question
                {
                    InstitutionId = inst.Id,
                    SubjectId = subject.Id,
                    TopicId = topic.Id,
                    SubTopicId = subTopic.Id,
                    LearningOutcomeId = lo.Id,
                    Content = $"Multi Q {a}-{qIdx}",
                    Status = QuestionStatus.Published
                };
                qMulti.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = qMulti.Id, QuestionStyleId = styleNewGen.Id });
                qMulti.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = qMulti.Id, QuestionStyleId = styleParagraph.Id });
                db.Add(qMulti);
                await db.SaveChangesAsync();

                db.Add(new StudentExamQuestionResult
                {
                    InstitutionId = inst.Id,
                    StudentExamAttemptId = attempt.Id,
                    QuestionId = qMulti.Id,
                    IsCorrect = false,
                    IsBlank = false
                });
            }

            for (int qIdx = 4; qIdx <= 6; qIdx++)
            {
                var qStrong = new Question
                {
                    InstitutionId = inst.Id,
                    SubjectId = subject.Id,
                    TopicId = topic.Id,
                    Content = $"Strong Q {a}-{qIdx}",
                    Status = QuestionStatus.Published
                };
                qStrong.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = qStrong.Id, QuestionStyleId = styleStrong.Id });
                db.Add(qStrong);
                await db.SaveChangesAsync();

                db.Add(new StudentExamQuestionResult
                {
                    InstitutionId = inst.Id,
                    StudentExamAttemptId = attempt.Id,
                    QuestionId = qStrong.Id,
                    IsCorrect = true,
                    IsBlank = false
                });
            }

            var qInsuff = new Question
            {
                InstitutionId = inst.Id,
                SubjectId = subject.Id,
                TopicId = topic.Id,
                Content = $"Insuff Q {a}",
                Status = QuestionStatus.Published
            };
            qInsuff.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = qInsuff.Id, QuestionStyleId = styleInsuff.Id });
            db.Add(qInsuff);
            await db.SaveChangesAsync();

            db.Add(new StudentExamQuestionResult
            {
                InstitutionId = inst.Id,
                StudentExamAttemptId = attempt.Id,
                QuestionId = qInsuff.Id,
                IsCorrect = false,
                IsBlank = false
            });

            await db.SaveChangesAsync();
        }

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);
        var styleAnalyticsHandler = new GetStudentQuestionStyleAnalyticsHandler(db, resourceAccess);
        var recHandler = new GetStudentAcademicRecommendationHandler(resourceAccess, mapHandler, styleAnalyticsHandler);

        var recs = await recHandler.GetRecommendationsAsync(new AcademicRecommendationQuery(studentId, ExamTypeId: examType.Id));

        // 1. LO is recommended and suppresses Topic and Subject
        recs.Should().ContainSingle(r => r.NodeType == RecommendationNodeType.LearningOutcome && r.NodeId == lo.Id);
        recs.Should().NotContain(r => r.NodeType == RecommendationNodeType.Topic && r.NodeId == topic.Id);
        recs.Should().NotContain(r => r.NodeType == RecommendationNodeType.Subject && r.NodeId == subject.Id);

        // 2. Both Critical styles (NEW_GENERATION and LONG_TEXT_PARAGRAPH) are recommended once each, without duplicates
        var styleRecs = recs.Where(r => r.NodeType == RecommendationNodeType.QuestionStyle).ToList();
        styleRecs.Should().HaveCount(2);
        styleRecs.Select(r => r.Code).Should().BeEquivalentTo(["LONG_TEXT_PARAGRAPH", "NEW_GENERATION"]);
        styleRecs.Should().OnlyContain(r => r.SubjectId == null && r.TopicId == null && r.LearningOutcomeId == null);

        // 3. Strong and InsufficientData styles are excluded
        styleRecs.Select(r => r.Code).Should().NotContain("CONCEPT_KNOWLEDGE");
        styleRecs.Select(r => r.Code).Should().NotContain("GRAPH_TABLE");
    }

    [Fact]
    public async Task AcceptStudyPlan_WithQuestionStyleSessions_PersistsQuestionStyleStudyTasksWithNullTaxonomy()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var styleId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var recommendations = new List<AcademicRecommendationDto>
        {
            new(
                AcademicRecommendationType.RemediateCritical,
                RecommendationNodeType.Topic,
                topicId,
                "MAT-PROB",
                "Problemler",
                subjectId,
                "Matematik",
                topicId,
                "Problemler",
                null,
                null,
                null,
                null,
                AcademicClassificationState.Critical,
                ClassificationBasis.OverallHistory,
                0.20m,
                0.20m,
                1.0m,
                TrendDirection.Decreasing,
                -0.15m,
                3,
                15,
                95,
                "v1",
                "v1"),
            new(
                AcademicRecommendationType.RemediateCritical,
                RecommendationNodeType.QuestionStyle,
                styleId,
                "NEW_GENERATION",
                "Yeni Nesil",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                AcademicClassificationState.Critical,
                ClassificationBasis.OverallHistory,
                0.25m,
                0.25m,
                1.0m,
                TrendDirection.Decreasing,
                null,
                3,
                12,
                93,
                "v1",
                "v1")
        };

        var startDate = new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        var generatedPlan = AcademicStudyPlanCalculator.CalculateStudyPlan(
            studentId,
            recommendations,
            planDays: 3,
            dailyStudyMinutes: 60,
            startDate: startDate);

        // Verify calculator invariants: 30-min sessions, 40% cap, round-robin
        generatedPlan.SessionMinutes.Should().Be(30);
        generatedPlan.TotalAvailableMinutes.Should().Be(180);
        var allSessions = generatedPlan.Days.SelectMany(d => d.Sessions).ToList();
        allSessions.Should().Contain(s => s.NodeType == RecommendationNodeType.QuestionStyle && s.NodeId == styleId);
        allSessions.Where(s => s.NodeId == styleId).Sum(s => s.DurationMinutes).Should().BeLessThanOrEqualTo(60); // 40% of 180 = 72 -> max 2 sessions (60m)

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var studyPlanHandler = Substitute.For<GetStudentAcademicStudyPlanHandler>(
            resourceAccess,
            Substitute.For<GetStudentAcademicRecommendationHandler>(
                resourceAccess,
                Substitute.For<GetStudentKnowledgeMapHandler>(
                    db,
                    resourceAccess,
                    Substitute.For<GetStudentAcademicClassificationHandler>(
                        Substitute.For<GetStudentAcademicTrendHandler>(db, Substitute.For<IAcademicCatalog>(), resourceAccess)))));

        studyPlanHandler.GetStudyPlanAsync(Arg.Any<AcademicStudyPlanQuery>(), Arg.Any<CancellationToken>())
            .Returns(generatedPlan);

        var acceptHandler = new AcceptAcademicStudyPlanHandler(db, currentUser, studentDirectory, studyPlanHandler);
        var result = await acceptHandler.HandleAsync(new AcceptAcademicStudyPlanCommand(studentId, PlanDays: 3, DailyStudyMinutes: 60, StartDate: startDate));

        result.Tasks.Should().Contain(t =>
            t.NodeType == RecommendationNodeType.QuestionStyle &&
            t.NodeId == styleId &&
            t.TitleSnapshot == "Yeni Nesil" &&
            t.SubjectId == null &&
            t.TopicId == null &&
            t.SubTopicId == null &&
            t.LearningOutcomeId == null);

        var persistedStyleTasks = await db.Set<StudyTask>()
            .Where(t => t.NodeType == RecommendationNodeType.QuestionStyle)
            .ToListAsync();

        persistedStyleTasks.Should().NotBeEmpty();
        persistedStyleTasks.Should().OnlyContain(t =>
            t.NodeId == styleId &&
            t.TitleSnapshot == "Yeni Nesil" &&
            t.SubjectId == null &&
            t.TopicId == null &&
            t.SubTopicId == null &&
            t.LearningOutcomeId == null);
    }

    [Theory]
    [InlineData("student")]
    [InlineData("coach")]
    [InlineData("parent")]
    public async Task DefaultInterpretationProvider_IntegratesQuestionStyles_ForAllAudiences(string audience)
    {
        var config = Substitute.For<IConfiguration>();
        var provider = new DefaultAcademicInterpretationProvider(config);

        var factPackage = new AcademicFactPackage(
            StudentId: Guid.NewGuid(),
            Audience: audience,
            Classifications:
            [
                new AcademicFactTopic(Guid.NewGuid(), "Üslü Sayılar", "Strong", 0.90m, 0.85m)
            ],
            Trends: [],
            Recommendations:
            [
                new AcademicFactRecommendation("Yeni Nesil", "RemediateCritical", 92)
            ],
            PlanSummary: new AcademicFactPlanSummary("Active", 2, 4, 0.50m),
            FactFingerprint: "fp-style-test",
            QuestionStyles:
            [
                new AcademicFactQuestionStyle(Guid.NewGuid(), "CONCEPT_KNOWLEDGE", "Kavram Bilgisi", "Strong", 0.88m, "Increasing"),
                new AcademicFactQuestionStyle(Guid.NewGuid(), "NEW_GENERATION", "Yeni Nesil", "Critical", 0.25m, "Decreasing")
            ]);

        var summary = await provider.InterpretFactsAsync(factPackage);

        summary.Should().NotBeNull();
        summary!.Audience.Should().Be(audience);
        summary.Strengths.Should().Contain("Üslü Sayılar");
        summary.Strengths.Should().Contain("Kavram Bilgisi (Soru Tarzı)");
        summary.ImprovementAreas.Should().Contain("Yeni Nesil (Soru Tarzı)");
        summary.TrendNotes.Should().Contain("Kavram Bilgisi (Soru Tarzı): Increasing");
        summary.TrendNotes.Should().Contain("Yeni Nesil (Soru Tarzı): Decreasing");
        summary.SuggestedFocus.Should().Contain("Yeni Nesil");
    }

    [Fact]
    public async Task GetStudentAcademicAiSummaryHandler_QuestionStyleChange_InvalidatesFactFingerprint()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var styleId = Guid.NewGuid();

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);
        var recHandler = new GetStudentAcademicRecommendationHandler(resourceAccess, mapHandler);

        var styleHandler = Substitute.For<GetStudentQuestionStyleAnalyticsHandler>(db, resourceAccess);

        // First call: NEW_GENERATION is Weak (0.45), GRAPH_TABLE is InsufficientData
        styleHandler.HandleAsync(Arg.Any<AcademicIntelligenceQuery>(), Arg.Any<CancellationToken>(), Arg.Any<int>())
            .Returns(new List<StudentQuestionStyleAnalyticsDto>
            {
                new(
                    styleId, "NEW_GENERATION", "Yeni Nesil",
                    TotalQuestions: 6, AttemptedQuestions: 6, CorrectCount: 3, IncorrectCount: 3, BlankCount: 0,
                    Accuracy: 0.50m, SuccessRate: 0.45m, Coverage: 1.0m, ObservationCount: 2,
                    ClassificationState: AcademicClassificationState.Weak,
                    TrendDirection: TrendDirection.Stable),
                new(
                    Guid.NewGuid(), "GRAPH_TABLE", "Grafik ve Tablo Yorumlama",
                    TotalQuestions: 2, AttemptedQuestions: 2, CorrectCount: 1, IncorrectCount: 1, BlankCount: 0,
                    Accuracy: 0.50m, SuccessRate: 0.45m, Coverage: 1.0m, ObservationCount: 1,
                    ClassificationState: AcademicClassificationState.InsufficientData,
                    TrendDirection: null)
            });

        AcademicFactPackage? capturedFirst = null;
        AcademicFactPackage? capturedSecond = null;

        var interpretationService = Substitute.For<IAcademicInterpretationService>();
        interpretationService.GenerateInterpretationAsync(Arg.Any<AcademicFactPackage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var pkg = callInfo.Arg<AcademicFactPackage>();
                if (capturedFirst == null) capturedFirst = pkg;
                else capturedSecond = pkg;
                return new AcademicAiSummaryDto(pkg.Audience, "Özet", [], [], [], [], true, DateTimeOffset.UtcNow);
            });

        var aiHandler = new GetStudentAcademicAiSummaryHandler(
            resourceAccess,
            classificationHandler,
            trendHandler,
            recHandler,
            interpretationService,
            db,
            styleHandler);

        await aiHandler.HandleAsync(studentId, "student");

        // Second call: NEW_GENERATION becomes Critical (0.20) with Decreasing trend
        styleHandler.HandleAsync(Arg.Any<AcademicIntelligenceQuery>(), Arg.Any<CancellationToken>(), Arg.Any<int>())
            .Returns(new List<StudentQuestionStyleAnalyticsDto>
            {
                new(
                    styleId, "NEW_GENERATION", "Yeni Nesil",
                    TotalQuestions: 8, AttemptedQuestions: 8, CorrectCount: 2, IncorrectCount: 6, BlankCount: 0,
                    Accuracy: 0.25m, SuccessRate: 0.20m, Coverage: 1.0m, ObservationCount: 3,
                    ClassificationState: AcademicClassificationState.Critical,
                    TrendDirection: TrendDirection.Decreasing)
            });

        await aiHandler.HandleAsync(studentId, "student");

        capturedFirst.Should().NotBeNull();
        capturedSecond.Should().NotBeNull();
        // InsufficientData style was filtered out of QuestionStyles fact list
        capturedFirst!.QuestionStyles.Should().ContainSingle(s => s.Code == "NEW_GENERATION");
        capturedFirst.FactFingerprint.Should().NotBe(capturedSecond!.FactFingerprint);
    }
}
