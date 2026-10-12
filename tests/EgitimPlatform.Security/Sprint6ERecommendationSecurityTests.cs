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
public class Sprint6ERecommendationSecurityTests(SecurityTestFactory factory)
{
    private string ConnectionString => factory.ConnectionString;

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetRecommendations_NonExistentStudent_ThrowsNotFound()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var nonExistentStudentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(nonExistentStudentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new NotFoundException("Student", nonExistentStudentId)));

        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);
        var recHandler = new GetStudentAcademicRecommendationHandler(resourceAccess, mapHandler);

        var query = new AcademicRecommendationQuery(nonExistentStudentId);

        await Assert.ThrowsAsync<NotFoundException>(() => recHandler.GetRecommendationsAsync(query));
    }

    [Fact]
    public async Task GetRecommendations_ForbiddenAccess_ThrowsForbidden()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var studentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new ForbiddenException("Teacher role cannot access student exam results.")));

        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);
        var recHandler = new GetStudentAcademicRecommendationHandler(resourceAccess, mapHandler);

        var query = new AcademicRecommendationQuery(studentId);

        await Assert.ThrowsAsync<ForbiddenException>(() => recHandler.GetRecommendationsAsync(query));
    }
}
