using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Sprint6FStudyPlanSecurityTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetStudyPlan_AuthorizedStudentSelf_AccessGranted()
    {
        var studentId = Guid.NewGuid();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, Guid.NewGuid(), Guid.NewGuid()));

        var recHandler = Substitute.ForPartsOf<GetStudentAcademicRecommendationHandler>(resourceAccess, Substitute.For<GetStudentKnowledgeMapHandler>(Db(), resourceAccess, Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(Db(), Substitute.For<IAcademicCatalog>(), resourceAccess))));
        var studyPlanHandler = new GetStudentAcademicStudyPlanHandler(resourceAccess, recHandler);

        var query = new AcademicStudyPlanQuery(studentId);
        var plan = await studyPlanHandler.GetStudyPlanAsync(query);

        Assert.NotNull(plan);
        Assert.Equal(studentId, plan.StudentId);
    }

    [Fact]
    public async Task GetStudyPlan_UnauthorizedStudentOther_ThrowsUnauthorizedAccessException()
    {
        var studentId = Guid.NewGuid();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new UnauthorizedAccessException("Access denied")));

        var recHandler = Substitute.ForPartsOf<GetStudentAcademicRecommendationHandler>(resourceAccess, Substitute.For<GetStudentKnowledgeMapHandler>(Db(), resourceAccess, Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(Db(), Substitute.For<IAcademicCatalog>(), resourceAccess))));
        var studyPlanHandler = new GetStudentAcademicStudyPlanHandler(resourceAccess, recHandler);

        var query = new AcademicStudyPlanQuery(studentId);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => studyPlanHandler.GetStudyPlanAsync(query));
    }

    [Fact]
    public async Task GetStudyPlan_TeacherAccess_FailClosedThrowsUnauthorizedAccessException()
    {
        var studentId = Guid.NewGuid();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        // Teachers fail-closed on student academic intelligence
        resourceAccess.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<StudentReferenceDto>(new UnauthorizedAccessException("Teachers are fail-closed for student academic intelligence")));

        var recHandler = Substitute.ForPartsOf<GetStudentAcademicRecommendationHandler>(resourceAccess, Substitute.For<GetStudentKnowledgeMapHandler>(Db(), resourceAccess, Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(Db(), Substitute.For<IAcademicCatalog>(), resourceAccess))));
        var studyPlanHandler = new GetStudentAcademicStudyPlanHandler(resourceAccess, recHandler);

        var query = new AcademicStudyPlanQuery(studentId);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => studyPlanHandler.GetStudyPlanAsync(query));
    }
}
