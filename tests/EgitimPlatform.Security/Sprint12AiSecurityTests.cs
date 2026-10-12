using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.AI.Features;
using EgitimPlatform.Modules.Exams.Services;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Sprint12AiSecurityTests
{
    [Fact]
    public async Task UnassignedCoach_GetAiSummary_ThrowsForbidden()
    {
        var studentId = Guid.NewGuid();

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.When(x => x.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>()))
            .Do(_ => throw new ForbiddenException("Coach is not assigned to this student."));

        var handler = new GetStudentAcademicAiSummaryHandler(
            resourceAccess,
            null!,
            null!,
            null!,
            null!,
            null!
        );

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId, "coach"));
    }

    [Fact]
    public async Task Teacher_GetAiSummary_FailClosed_ThrowsForbidden()
    {
        var studentId = Guid.NewGuid();

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.When(x => x.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>()))
            .Do(_ => throw new ForbiddenException("Teacher role cannot access student academic intelligence."));

        var handler = new GetStudentAcademicAiSummaryHandler(
            resourceAccess,
            null!,
            null!,
            null!,
            null!,
            null!
        );

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId, "student"));
    }
}
