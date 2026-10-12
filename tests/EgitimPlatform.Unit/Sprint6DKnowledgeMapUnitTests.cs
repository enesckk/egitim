using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint6DKnowledgeMapUnitTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetKnowledgeMap_FullHierarchyAssembly_Assembles4TierTree()
    {
        using var db = Db();
        var examType = new ExamType { Code = "TYT", Name = "TYT" };
        var subject = new Subject { Code = "MAT", Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = "MAT-TOP-1", Name = "Türev", SubjectId = subject.Id };
        var subTopic = new SubTopic { Code = "MAT-SUB-1", Name = "Türev Kuralları", TopicId = topic.Id };
        var lo = new LearningOutcome { Code = "MAT.11.1", Name = "Türev Alma", SubTopicId = subTopic.Id };

        db.AddRange(examType, subject, topic, subTopic, lo);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);

        var query = new AcademicClassificationQuery(studentId, ExamTypeId: examType.Id);
        var km = await mapHandler.GetKnowledgeMapAsync(query);

        Assert.Equal(studentId, km.StudentId);
        Assert.Equal(AcademicClassificationPolicy.CurrentPolicyVersion, km.PolicyVersion);
        Assert.Single(km.Subjects);

        var subNode = km.Subjects[0];
        Assert.Equal(subject.Id, subNode.Id);
        Assert.Single(subNode.Topics);

        var topNode = subNode.Topics[0];
        Assert.Equal(topic.Id, topNode.Id);
        Assert.Equal(subject.Id, topNode.SubjectId);

        Assert.Single(topNode.SubTopics);
        var stNode = topNode.SubTopics[0];
        Assert.Equal(subTopic.Id, stNode.Id);
        Assert.Equal(topic.Id, stNode.TopicId);

        Assert.Single(stNode.LearningOutcomes);
        var loNode = stNode.LearningOutcomes[0];
        Assert.Equal(lo.Id, loNode.Id);
        Assert.Equal(subTopic.Id, loNode.SubTopicId);
        Assert.Equal(topic.Id, loNode.TopicId);
        Assert.Equal(subject.Id, loNode.SubjectId);
    }

    [Fact]
    public async Task GetKnowledgeMap_NoGuidEmptySentinels_AssertsNoParentOrNodeUsesGuidEmpty()
    {
        using var db = Db();
        var examType = new ExamType { Code = "AYT", Name = "AYT" };
        var subject = new Subject { Code = "FIZ", Name = "Fizik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = "FIZ-TOP-1", Name = "Vektörler", SubjectId = subject.Id };
        var subTopic = new SubTopic { Code = "FIZ-SUB-1", Name = "Bileşke Vektör", TopicId = topic.Id };
        var lo = new LearningOutcome { Code = "FIZ.11.1", Name = "Vektör Toplama", SubTopicId = subTopic.Id };

        db.AddRange(examType, subject, topic, subTopic, lo);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);

        var query = new AcademicClassificationQuery(studentId, ExamTypeId: examType.Id);
        var km = await mapHandler.GetKnowledgeMapAsync(query);

        Assert.All(km.Subjects, s =>
        {
            Assert.NotEqual(Guid.Empty, s.Id);
            Assert.All(s.Topics, t =>
            {
                Assert.NotEqual(Guid.Empty, t.Id);
                Assert.NotEqual(Guid.Empty, t.SubjectId);
                Assert.All(t.SubTopics, st =>
                {
                    Assert.NotEqual(Guid.Empty, st.Id);
                    Assert.NotEqual(Guid.Empty, st.TopicId);
                    Assert.All(st.LearningOutcomes, l =>
                    {
                        Assert.NotEqual(Guid.Empty, l.Id);
                        Assert.NotEqual(Guid.Empty, l.SubTopicId);
                        Assert.NotEqual(Guid.Empty, l.TopicId);
                        Assert.NotEqual(Guid.Empty, l.SubjectId);
                    });
                });
            });
        });
    }

    [Fact]
    public async Task GetKnowledgeMap_UntestedNode_ReturnsHasEvidenceFalseAndNullRates()
    {
        using var db = Db();
        var examType = new ExamType { Code = "AYT", Name = "AYT" };
        var subject = new Subject { Code = "FIZ", Name = "Fizik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = "FIZ-TOP-1", Name = "Vektörler", SubjectId = subject.Id };
        var subTopic = new SubTopic { Code = "FIZ-SUB-1", Name = "Bileşke Vektör", TopicId = topic.Id };
        var lo = new LearningOutcome { Code = "FIZ.11.1", Name = "Vektör Toplama", SubTopicId = subTopic.Id };

        db.AddRange(examType, subject, topic, subTopic, lo);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);

        var query = new AcademicClassificationQuery(studentId, ExamTypeId: examType.Id);
        var km = await mapHandler.GetKnowledgeMapAsync(query);

        var subNode = km.Subjects[0];
        var loNode = subNode.Topics[0].SubTopics[0].LearningOutcomes[0];

        Assert.False(loNode.HasEvidence);
        Assert.False(loNode.HasEnoughData);
        Assert.Equal(AcademicClassificationState.InsufficientData, loNode.State);
        Assert.Null(loNode.SuccessRate);
        Assert.Null(loNode.Accuracy);
        Assert.Null(loNode.Coverage);
        Assert.Equal(0, loNode.TotalQuestions);
        Assert.Equal(0, loNode.ObservationCount);
    }

    [Fact]
    public async Task GetKnowledgeMap_SubTopicDirectEvidenceWithoutLO_SetsSubTopicHasEvidenceTrue()
    {
        using var db = Db();
        var examType = new ExamType { Code = "TYT", Name = "TYT" };
        var subject = new Subject { Code = "KIM", Name = "Kimya", ExamTypeId = examType.Id };
        var topic = new Topic { Code = "KIM-TOP-1", Name = "Atom", SubjectId = subject.Id };
        var subTopic = new SubTopic { Code = "KIM-SUB-1", Name = "Atom Modelleri", TopicId = topic.Id };

        db.AddRange(examType, subject, topic, subTopic);
        await db.SaveChangesAsync();

        var inst = new Institution { Name = "Inst Test SubTopic" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var exam = new Exam { InstitutionId = inst.Id, Title = "Kimya Deneme 1", ExamTypeId = examType.Id, ExamDate = now };
        db.Add(exam);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = studentId, TakenAt = exam.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = true };
        db.Add(attempt);
        await db.SaveChangesAsync();

        // Direct SubTopic question evidence without LearningOutcomeId (LearningOutcomeId == null)
        var question = new Question { SubjectId = subject.Id, TopicId = topic.Id, SubTopicId = subTopic.Id, LearningOutcomeId = null, Content = "Atom modeli hangisidir?" };
        db.Add(question);
        await db.SaveChangesAsync();

        var qr = new StudentExamQuestionResult { StudentExamAttemptId = attempt.Id, QuestionId = question.Id, IsCorrect = true, IsBlank = false };
        db.Add(qr);
        await db.SaveChangesAsync();

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);

        var query = new AcademicClassificationQuery(studentId, ExamTypeId: examType.Id);
        var km = await mapHandler.GetKnowledgeMapAsync(query);

        var subTopicNode = km.Subjects[0].Topics[0].SubTopics[0];
        Assert.True(subTopicNode.HasEvidence);
        Assert.Empty(subTopicNode.LearningOutcomes);
    }

    [Fact]
    public async Task GetKnowledgeMap_ExamTypeIsolation_ReturnsOnlyTargetExamTypeTaxonomy()
    {
        using var db = Db();
        var examTypeA = new ExamType { Code = "TYT", Name = "TYT" };
        var examTypeB = new ExamType { Code = "AYT", Name = "AYT" };

        var subjectA = new Subject { Code = "MAT-TYT", Name = "Matematik TYT", ExamTypeId = examTypeA.Id };
        var subjectB = new Subject { Code = "MAT-AYT", Name = "Matematik AYT", ExamTypeId = examTypeB.Id };

        db.AddRange(examTypeA, examTypeB, subjectA, subjectB);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);

        var query = new AcademicClassificationQuery(studentId, ExamTypeId: examTypeA.Id);
        var km = await mapHandler.GetKnowledgeMapAsync(query);

        Assert.Single(km.Subjects);
        Assert.Equal(subjectA.Id, km.Subjects[0].Id);
        Assert.DoesNotContain(km.Subjects, s => s.Id == subjectB.Id);
    }

    [Fact]
    public async Task GetKnowledgeMap_NoExamTypeAndNoEvidence_ReturnsEmptySubjectList()
    {
        using var db = Db();
        var examType = new ExamType { Code = "LGS", Name = "LGS" };
        var subject = new Subject { Code = "TUR", Name = "Türkçe", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);

        var query = new AcademicClassificationQuery(studentId, ExamTypeId: null);
        var km = await mapHandler.GetKnowledgeMapAsync(query);

        Assert.Empty(km.Subjects);
        Assert.Equal(0, km.Summary.TotalSubjects);
    }
}
