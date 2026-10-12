using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Security.Fixtures;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Sprint6ASecurityTests(SecurityTestFactory factory)
{
    private string ConnectionString => factory.ConnectionString;
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetSubjectPerformance_NonExistentStudent_ThrowsNotFound()
    {
        using var db = Db();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateStudentReadAccessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new NotFoundException("Student", Guid.NewGuid())));

        var handler = new GetStudentAcademicIntelligenceHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);

        var nonExistentStudentId = Guid.NewGuid();
        var query = new AcademicIntelligenceQuery(nonExistentStudentId);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.GetSubjectPerformanceAsync(query));
        Assert.NotNull(ex);
    }
}
