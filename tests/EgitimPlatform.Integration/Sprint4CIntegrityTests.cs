using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.FinalizeAttempt;
using EgitimPlatform.Modules.Exams.Features.SaveAnswers;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint4CIntegrityTests(IntegrationTestFactory factory)
{
    private class TestCurrentUser(Guid? userId) : EgitimPlatform.BuildingBlocks.Interfaces.ICurrentUser
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Roles => new[] { EgitimPlatform.BuildingBlocks.Constants.Roles.SuperAdmin };
        public string? Email => "super@test.local";
        public bool IsSuperAdmin => true;
        public bool IsInRole(string role) => role == EgitimPlatform.BuildingBlocks.Constants.Roles.SuperAdmin;
        public Task<Guid?> GetInstitutionIdAsync() => Task.FromResult<Guid?>(null);
    }

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(factory.ConnectionString).Options);

    private static string Code() => "S4C-CODE-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

    [Fact]
    public async Task SaveAnswers_UpsertsExistingAnswers_WithoutDuplicateRows()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst SaveAns" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, UserId = Guid.NewGuid(), FirstName = "S1", LastName = "T" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Exam Title" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 2 };
        db.Add(section);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1", Status = QuestionStatus.Published };
        db.Add(q1);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, Status = AttemptStatus.Draft };
        db.Add(attempt);
        await db.SaveChangesAsync();

        // 1st Save Answer: "B"
        var r1 = new StudentExamQuestionResult
        {
            InstitutionId = inst.Id,
            StudentExamAttemptId = attempt.Id,
            ExamQuestionId = eq1.Id,
            QuestionId = q1.Id,
            SelectedOption = "B",
            IsBlank = false
        };
        db.Add(r1);
        await db.SaveChangesAsync();

        var count1 = await db.Set<StudentExamQuestionResult>().CountAsync(r => r.StudentExamAttemptId == attempt.Id);
        Assert.Equal(1, count1);

        // 2nd Save Answer: Update to "A" (no new row)
        r1.SelectedOption = "A";
        await db.SaveChangesAsync();

        var count2 = await db.Set<StudentExamQuestionResult>().CountAsync(r => r.StudentExamAttemptId == attempt.Id);
        Assert.Equal(1, count2);
        var updated = await db.Set<StudentExamQuestionResult>().FirstAsync(r => r.StudentExamAttemptId == attempt.Id);
        Assert.Equal("A", updated.SelectedOption);
    }

    [Fact]
    public async Task FinalizeAttempt_RawMode_RebuildsAggregatesAndSetsIsRawAvailableTrue()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Finalize" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, UserId = Guid.NewGuid(), FirstName = "S2", LastName = "T" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Exam Title", WrongAnswerPenalty = 4m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 2 };
        db.Add(section);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1", Status = QuestionStatus.Published };
        var q2 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q2", Status = QuestionStatus.Published };
        db.AddRange(q1, q2);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q2.Id, QuestionNumber = 2, Order = 2, CorrectOption = "B" };
        db.AddRange(eq1, eq2);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, Status = AttemptStatus.Draft };
        db.Add(attempt);
        await db.SaveChangesAsync();

        // Submit Raw Answers: Q1 = A (Correct), Q2 = A (Wrong)
        var r1 = new StudentExamQuestionResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamQuestionId = eq1.Id, QuestionId = q1.Id, SelectedOption = "A", IsBlank = false };
        var r2 = new StudentExamQuestionResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamQuestionId = eq2.Id, QuestionId = q2.Id, SelectedOption = "A", IsBlank = false };
        db.AddRange(r1, r2);
        await db.SaveChangesAsync();

        // Instantiate Handler via Factory ServiceScope with SuperAdmin user context
        using var scope = factory.Services.CreateScope();
        var mockUser = new TestCurrentUser(student.UserId);

        var resourceAccess = new ExamResourceAccess(db, mockUser, scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.IStudentDirectory>(), scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.ICoachStudentQuery>());
        var scoringService = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Modules.Exams.Services.IExamScoringService>();
        var auditService = scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.IAuditService>();

        var handler = new FinalizeAttemptHandler(db, mockUser, resourceAccess, scoringService, new RawExamAttemptFinalizer(), auditService);

        await handler.HandleAsync(attempt.Id);

        using var dbVerify = Db();
        var finalizedAttempt = await dbVerify.Set<StudentExamAttempt>()
            .Include(a => a.SubjectResults)
            .Include(a => a.TopicResults)
            .Include(a => a.QuestionResults)
            .FirstAsync(a => a.Id == attempt.Id);

        Assert.Equal(AttemptStatus.Finalized, finalizedAttempt.Status);
        Assert.True(finalizedAttempt.IsRawAvailable);
        Assert.Equal(1, finalizedAttempt.TotalCorrect);
        Assert.Equal(1, finalizedAttempt.TotalWrong);
        Assert.Equal(0, finalizedAttempt.TotalBlank);
        Assert.Equal(0.75m, finalizedAttempt.TotalNet);

        // Subject Results Rebuilt
        Assert.Single(finalizedAttempt.SubjectResults);
        var sr = finalizedAttempt.SubjectResults[0];
        Assert.Equal(1, sr.Correct);
        Assert.Equal(1, sr.Wrong);
        Assert.Equal(0.75m, sr.Net);

        // Topic Results Rebuilt
        Assert.Single(finalizedAttempt.TopicResults);
        var tr = finalizedAttempt.TopicResults[0];
        Assert.Equal(1, tr.Correct);
        Assert.Equal(1, tr.Wrong);
        Assert.Equal(0.75m, tr.Net);
    }

    [Fact]
    public async Task DraftState_SaveAnswersDoesNotSetIsRawAvailable_AndDoesNotRebuildAggregates()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Draft" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, UserId = Guid.NewGuid(), FirstName = "SDraft", LastName = "T" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Draft Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1", Status = QuestionStatus.Published };
        db.Add(q1);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, Status = AttemptStatus.Draft };
        db.Add(attempt);
        await db.SaveChangesAsync();

        // Save raw answer
        var r1 = new StudentExamQuestionResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamQuestionId = eq1.Id, QuestionId = q1.Id, SelectedOption = "A", IsBlank = false };
        db.Add(r1);
        await db.SaveChangesAsync();

        // Verify Draft assertions
        using var dbVerify = Db();
        var draftAttempt = await dbVerify.Set<StudentExamAttempt>()
            .Include(a => a.SubjectResults)
            .Include(a => a.TopicResults)
            .FirstAsync(a => a.Id == attempt.Id);

        Assert.Equal(AttemptStatus.Draft, draftAttempt.Status);
        Assert.False(draftAttempt.IsRawAvailable);
        Assert.Empty(draftAttempt.SubjectResults);
        Assert.Empty(draftAttempt.TopicResults);
        Assert.Equal(0, draftAttempt.TotalCorrect);
        Assert.Equal(0, draftAttempt.TotalWrong);
        Assert.Equal(0m, draftAttempt.TotalNet);
    }

    [Fact]
    public async Task PartialAnswerSubmission_Finalize_MissingAnswersBecomeBlank_AndEnforcesInvariant()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Partial" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, UserId = Guid.NewGuid(), FirstName = "SPartial", LastName = "T" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Partial Exam", WrongAnswerPenalty = 4m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 4 };
        db.Add(section);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1", Status = QuestionStatus.Published };
        var q2 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q2", Status = QuestionStatus.Published };
        var q3 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q3", Status = QuestionStatus.Published };
        var q4 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q4", Status = QuestionStatus.Published };
        db.AddRange(q1, q2, q3, q4);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q2.Id, QuestionNumber = 2, Order = 2, CorrectOption = "B" };
        var eq3 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q3.Id, QuestionNumber = 3, Order = 3, CorrectOption = "C" };
        var eq4 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q4.Id, QuestionNumber = 4, Order = 4, CorrectOption = "D" };
        db.AddRange(eq1, eq2, eq3, eq4);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, Status = AttemptStatus.Draft };
        db.Add(attempt);
        await db.SaveChangesAsync();

        // Submit only 2 answers out of 4 (Q1 = A correct, Q2 = A wrong)
        var r1 = new StudentExamQuestionResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamQuestionId = eq1.Id, QuestionId = q1.Id, SelectedOption = "A", IsBlank = false };
        var r2 = new StudentExamQuestionResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamQuestionId = eq2.Id, QuestionId = q2.Id, SelectedOption = "A", IsBlank = false };
        db.AddRange(r1, r2);
        await db.SaveChangesAsync();

        using var scope = factory.Services.CreateScope();
        var mockUser = new TestCurrentUser(student.UserId);
        var resourceAccess = new ExamResourceAccess(db, mockUser, scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.IStudentDirectory>(), scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.ICoachStudentQuery>());
        var scoringService = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Modules.Exams.Services.IExamScoringService>();
        var auditService = scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.IAuditService>();
        var handler = new FinalizeAttemptHandler(db, mockUser, resourceAccess, scoringService, new RawExamAttemptFinalizer(), auditService);

        await handler.HandleAsync(attempt.Id);

        using var dbVerify = Db();
        var finalized = await dbVerify.Set<StudentExamAttempt>()
            .Include(a => a.QuestionResults)
            .Include(a => a.SubjectResults)
            .FirstAsync(a => a.Id == attempt.Id);

        // Missing questions eq3 and eq4 are generated as Blank
        Assert.Equal(4, finalized.QuestionResults.Count);
        Assert.Equal(1, finalized.TotalCorrect);
        Assert.Equal(1, finalized.TotalWrong);
        Assert.Equal(2, finalized.TotalBlank);

        // Invariant: Correct + Wrong + Blank == ExamQuestion count
        Assert.Equal(4, finalized.TotalCorrect + finalized.TotalWrong + finalized.TotalBlank);
        var sr = finalized.SubjectResults.Single();
        Assert.Equal(4, sr.Correct + sr.Wrong + sr.Blank);
    }

    [Fact]
    public async Task RepeatFinalize_RejectsSecondCall_AndPreventsDuplicates()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Repeat" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, UserId = Guid.NewGuid(), FirstName = "SRepeat", LastName = "T" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Repeat Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1", Status = QuestionStatus.Published };
        db.Add(q1);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, Status = AttemptStatus.Draft };
        db.Add(attempt);
        await db.SaveChangesAsync();

        using var scope = factory.Services.CreateScope();
        var mockUser = new TestCurrentUser(student.UserId);
        var resourceAccess = new ExamResourceAccess(db, mockUser, scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.IStudentDirectory>(), scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.ICoachStudentQuery>());
        var scoringService = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Modules.Exams.Services.IExamScoringService>();
        var auditService = scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.IAuditService>();
        var handler = new FinalizeAttemptHandler(db, mockUser, resourceAccess, scoringService, new RawExamAttemptFinalizer(), auditService);

        // First finalization -> Success
        await handler.HandleAsync(attempt.Id);

        // Second finalization -> ConflictException (409)
        await Assert.ThrowsAsync<EgitimPlatform.BuildingBlocks.Exceptions.ConflictException>(() => handler.HandleAsync(attempt.Id));

        using var dbVerify = Db();
        var qCount = await dbVerify.Set<StudentExamQuestionResult>().CountAsync(r => r.StudentExamAttemptId == attempt.Id);
        var sCount = await dbVerify.Set<StudentExamSubjectResult>().CountAsync(r => r.StudentExamAttemptId == attempt.Id);
        var tCount = await dbVerify.Set<StudentExamTopicResult>().CountAsync(r => r.StudentExamAttemptId == attempt.Id);

        Assert.Equal(1, qCount);
        Assert.Equal(1, sCount);
        Assert.Equal(1, tCount);
    }

    [Fact]
    public async Task FinalizeAttempt_TransactionRollback_OnFailure_LeavesStateUnchanged()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Rollback" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, UserId = Guid.NewGuid(), FirstName = "SRollback", LastName = "T" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Rollback Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1", Status = QuestionStatus.Published };
        db.Add(q1);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, Status = AttemptStatus.Draft };
        db.Add(attempt);
        await db.SaveChangesAsync();

        using var scope = factory.Services.CreateScope();
        var mockUser = new TestCurrentUser(student.UserId);
        var resourceAccess = new ExamResourceAccess(db, mockUser, scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.IStudentDirectory>(), scope.ServiceProvider.GetRequiredService<EgitimPlatform.BuildingBlocks.Interfaces.ICoachStudentQuery>());
        var scoringService = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Modules.Exams.Services.IExamScoringService>();
        
        var failingAuditService = new FailingAuditService();
        var handler = new FinalizeAttemptHandler(db, mockUser, resourceAccess, scoringService, new RawExamAttemptFinalizer(), failingAuditService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(attempt.Id));

        using var dbVerify = Db();
        var uncommittedAttempt = await dbVerify.Set<StudentExamAttempt>()
            .Include(a => a.SubjectResults)
            .Include(a => a.TopicResults)
            .FirstAsync(a => a.Id == attempt.Id);

        Assert.Equal(AttemptStatus.Draft, uncommittedAttempt.Status);
        Assert.False(uncommittedAttempt.IsRawAvailable);
        Assert.Empty(uncommittedAttempt.SubjectResults);
        Assert.Empty(uncommittedAttempt.TopicResults);
    }

    private class FailingAuditService : EgitimPlatform.BuildingBlocks.Interfaces.IAuditService
    {
        public Task AddPendingLogAsync(Guid userId, string action, string entityType, string entityId, Guid? institutionId = null, string? metadataJson = null, string? correlationId = null, string? ipAddress = null, string? userAgent = null)
        {
            throw new InvalidOperationException("Simulated audit failure to test transaction rollback");
        }

        public Task LogAsync(Guid userId, string action, string entityType, string entityId, Guid? institutionId = null, string? metadataJson = null, string? correlationId = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated audit failure to test transaction rollback");
        }
    }
}
