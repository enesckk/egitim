using Xunit;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.AI.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace EgitimPlatform.Unit;

public class Sprint12AiUnitTests
{
    [Fact]
    public async Task DefaultProvider_GeneratesStudentSummary_FromFacts()
    {
        var config = Substitute.For<IConfiguration>();
        var section = Substitute.For<IConfigurationSection>();
        section.Value.Returns("true");
        config.GetSection("AiSettings:Enabled").Returns(section);

        var provider = new DefaultAcademicInterpretationProvider(config);

        var factPackage = new AcademicFactPackage(
            StudentId: Guid.NewGuid(),
            Audience: "student",
            Classifications: new[]
            {
                new AcademicFactTopic(Guid.NewGuid(), "Üslü Sayılar", "Strong", 0.9m, 0.85m),
                new AcademicFactTopic(Guid.NewGuid(), "Köklü Sayılar", "Critical", 0.3m, 0.25m)
            },
            Trends: new[]
            {
                new AcademicFactTrend("Matematik", "Increasing", "Matematik genel eğilimi: Yükselişte")
            },
            Recommendations: new[]
            {
                new AcademicFactRecommendation("Köklü Sayılar Çalışması", "RemediateCritical", 100)
            },
            PlanSummary: new AcademicFactPlanSummary("Active", 5, 10, 0.5m),
            FactFingerprint: "test-fingerprint"
        );

        var result = await provider.InterpretFactsAsync(factPackage);
        result.Should().NotBeNull();
        result!.IsAvailable.Should().BeTrue();
        result.Audience.Should().Be("student");
        result.Strengths.Should().Contain("Üslü Sayılar");
        result.ImprovementAreas.Should().Contain("Köklü Sayılar");
        result.SuggestedFocus.Should().Contain("Köklü Sayılar Çalışması");
    }

    [Fact]
    public async Task DefaultProvider_HandlesInsufficientData_Gracefully()
    {
        var config = Substitute.For<IConfiguration>();
        var provider = new DefaultAcademicInterpretationProvider(config);

        var factPackage = new AcademicFactPackage(
            StudentId: Guid.NewGuid(),
            Audience: "parent",
            Classifications: Array.Empty<AcademicFactTopic>(),
            Trends: Array.Empty<AcademicFactTrend>(),
            Recommendations: Array.Empty<AcademicFactRecommendation>(),
            PlanSummary: null,
            FactFingerprint: "empty-fingerprint"
        );

        var result = await provider.InterpretFactsAsync(factPackage);
        result.Should().NotBeNull();
        result!.Summary.Should().Contain("yeterli");
        result.Strengths.Should().BeEmpty();
        result.ImprovementAreas.Should().BeEmpty();
    }

    [Fact]
    public async Task Orchestrator_CachesResponse_ByFingerprint()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mockProvider = Substitute.For<IAiLlmProvider>();
        
        var factPackage = new AcademicFactPackage(
            StudentId: Guid.NewGuid(),
            Audience: "coach",
            Classifications: Array.Empty<AcademicFactTopic>(),
            Trends: Array.Empty<AcademicFactTrend>(),
            Recommendations: Array.Empty<AcademicFactRecommendation>(),
            PlanSummary: null,
            FactFingerprint: "fp123"
        );

        var expectedDto = new AcademicAiSummaryDto("coach", "Koç özeti", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), true, DateTimeOffset.UtcNow);
        mockProvider.InterpretFactsAsync(factPackage, Arg.Any<CancellationToken>()).Returns(Task.FromResult<AcademicAiSummaryDto?>(expectedDto));

        var service = new AcademicInterpretationService(mockProvider, cache, NullLogger<AcademicInterpretationService>.Instance);

        var firstCall = await service.GenerateInterpretationAsync(factPackage);
        var secondCall = await service.GenerateInterpretationAsync(factPackage);

        firstCall.Summary.Should().Be("Koç özeti");
        secondCall.Summary.Should().Be("Koç özeti");
        await mockProvider.Received(1).InterpretFactsAsync(factPackage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Orchestrator_ReturnsFallback_WhenProviderFails()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mockProvider = Substitute.For<IAiLlmProvider>();
        
        var factPackage = new AcademicFactPackage(
            StudentId: Guid.NewGuid(),
            Audience: "student",
            Classifications: Array.Empty<AcademicFactTopic>(),
            Trends: Array.Empty<AcademicFactTrend>(),
            Recommendations: Array.Empty<AcademicFactRecommendation>(),
            PlanSummary: null,
            FactFingerprint: "error-fp"
        );

        mockProvider.InterpretFactsAsync(factPackage, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AcademicAiSummaryDto?>(new System.Exception("API failure")));

        var service = new AcademicInterpretationService(mockProvider, cache, NullLogger<AcademicInterpretationService>.Instance);

        var result = await service.GenerateInterpretationAsync(factPackage);
        result.IsAvailable.Should().BeFalse();
        result.Summary.Should().Contain("kullanılamıyor");
    }

    [Fact]
    public void ClassificationStates_AreStrictlyDefined_AndDoNotContainMastered()
    {
        var validStates = Enum.GetNames(typeof(EgitimPlatform.Modules.Exams.Features.AcademicIntelligence.AcademicClassificationState));
        validStates.Should().BeEquivalentTo(new[] { "InsufficientData", "Critical", "Weak", "Developing", "Strong" });
        validStates.Should().NotContain("Mastered");
    }
}
