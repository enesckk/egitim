using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Sprint7ASecurityTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task AcceptProposal_StudentOther_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(false);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Student).Returns(true);
        currentUser.UserId.Returns(currentUserId); // Different from studentUserId

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, studentUserId));

        var studyPlanHandler = Substitute.For<GetStudentAcademicStudyPlanHandler>(
            Substitute.For<IExamResourceAccess>(),
            Substitute.For<GetStudentAcademicRecommendationHandler>(
                Substitute.For<IExamResourceAccess>(),
                Substitute.For<GetStudentKnowledgeMapHandler>(db, Substitute.For<IExamResourceAccess>(), Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(db, Substitute.For<IAcademicCatalog>(), Substitute.For<IExamResourceAccess>())))
            )
        );

        var handler = new AcceptAcademicStudyPlanHandler(db, currentUser, studentDirectory, studyPlanHandler);

        var command = new AcceptAcademicStudyPlanCommand(studentId);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(command));
    }

    [Fact]
    public async Task AcceptProposal_CrossInstitution_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var otherInstId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(false);
        currentUser.GetInstitutionIdAsync().Returns(otherInstId); // Cross-tenant boundary

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, studentUserId));

        var studyPlanHandler = Substitute.For<GetStudentAcademicStudyPlanHandler>(
            Substitute.For<IExamResourceAccess>(),
            Substitute.For<GetStudentAcademicRecommendationHandler>(
                Substitute.For<IExamResourceAccess>(),
                Substitute.For<GetStudentKnowledgeMapHandler>(db, Substitute.For<IExamResourceAccess>(), Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(db, Substitute.For<IAcademicCatalog>(), Substitute.For<IExamResourceAccess>())))
            )
        );

        var handler = new AcceptAcademicStudyPlanHandler(db, currentUser, studentDirectory, studyPlanHandler);

        var command = new AcceptAcademicStudyPlanCommand(studentId);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(command));
    }

    [Fact]
    public async Task AcceptProposal_TeacherRole_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(false);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Teacher).Returns(true); // Teachers fail closed for study plans

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, studentUserId));

        var studyPlanHandler = Substitute.For<GetStudentAcademicStudyPlanHandler>(
            Substitute.For<IExamResourceAccess>(),
            Substitute.For<GetStudentAcademicRecommendationHandler>(
                Substitute.For<IExamResourceAccess>(),
                Substitute.For<GetStudentKnowledgeMapHandler>(db, Substitute.For<IExamResourceAccess>(), Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(db, Substitute.For<IAcademicCatalog>(), Substitute.For<IExamResourceAccess>())))
            )
        );

        var handler = new AcceptAcademicStudyPlanHandler(db, currentUser, studentDirectory, studyPlanHandler);

        var command = new AcceptAcademicStudyPlanCommand(studentId);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(command));
    }
}
