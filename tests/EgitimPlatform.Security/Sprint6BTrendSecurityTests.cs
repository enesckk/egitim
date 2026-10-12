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
public class Sprint6BTrendSecurityTests(SecurityTestFactory factory)
{
    private string ConnectionString => factory.ConnectionString;

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetSubjectTrends_NonExistentStudent_ThrowsNotFound()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var nonExistentStudentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(nonExistentStudentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new NotFoundException("Student", nonExistentStudentId)));

        var handler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var query = new AcademicTrendQuery(nonExistentStudentId);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.GetSubjectTrendsAsync(query));
    }

    [Fact]
    public async Task GetTopicTrends_NonExistentStudent_ThrowsNotFound()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var nonExistentStudentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(nonExistentStudentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new NotFoundException("Student", nonExistentStudentId)));

        var handler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var query = new AcademicTrendQuery(nonExistentStudentId);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.GetTopicTrendsAsync(query));
    }

    [Fact]
    public async Task GetLearningOutcomeTrends_NonExistentStudent_ThrowsNotFound()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var nonExistentStudentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(nonExistentStudentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new NotFoundException("Student", nonExistentStudentId)));

        var handler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var query = new AcademicTrendQuery(nonExistentStudentId);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.GetLearningOutcomeTrendsAsync(query));
    }

    [Fact]
    public async Task GetSubjectTrends_ForbiddenAccess_ThrowsForbidden()
    {
        _ = ConnectionString;
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var studentId = Guid.NewGuid();
        resourceAccess.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new ForbiddenException("Teacher role cannot access student exam results.")));

        var handler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var query = new AcademicTrendQuery(studentId);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.GetSubjectTrendsAsync(query));
    }
}
