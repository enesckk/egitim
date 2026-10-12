using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Security.Fixtures;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Sprint6CClassificationSecurityTests(SecurityTestFactory factory)
{
    private string ConnectionString => factory.ConnectionString;

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetSubjectClassifications_NonExistentStudent_ThrowsNotFound()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var nonExistentStudentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(nonExistentStudentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new NotFoundException("Student", nonExistentStudentId)));

        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);

        var query = new AcademicClassificationQuery(nonExistentStudentId);

        await Assert.ThrowsAsync<NotFoundException>(() => classificationHandler.GetSubjectClassificationsAsync(query));
    }

    [Fact]
    public async Task GetTopicClassifications_NonExistentStudent_ThrowsNotFound()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var nonExistentStudentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(nonExistentStudentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new NotFoundException("Student", nonExistentStudentId)));

        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);

        var query = new AcademicClassificationQuery(nonExistentStudentId);

        await Assert.ThrowsAsync<NotFoundException>(() => classificationHandler.GetTopicClassificationsAsync(query));
    }

    [Fact]
    public async Task GetLearningOutcomeClassifications_ForbiddenAccess_ThrowsForbidden()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var studentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new ForbiddenException("Teacher role cannot access student exam results.")));

        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);

        var query = new AcademicClassificationQuery(studentId);

        await Assert.ThrowsAsync<ForbiddenException>(() => classificationHandler.GetLearningOutcomeClassificationsAsync(query));
    }
}
