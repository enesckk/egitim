using EgitimPlatform.BuildingBlocks.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EgitimPlatform.Modules.AI.Services;

public class AcademicInterpretationService(
    IAiLlmProvider provider,
    IMemoryCache cache,
    ILogger<AcademicInterpretationService> logger) : IAcademicInterpretationService
{
    public async Task<AcademicAiSummaryDto> GenerateInterpretationAsync(AcademicFactPackage factPackage, CancellationToken ct = default)
    {
        var cacheKey = $"ai-summary:{factPackage.StudentId}:{factPackage.Audience}:{factPackage.FactFingerprint}";

        if (cache.TryGetValue<AcademicAiSummaryDto>(cacheKey, out var cachedDto) && cachedDto != null)
        {
            return cachedDto;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5-second bounded execution

            var dto = await provider.InterpretFactsAsync(factPackage, cts.Token);

            if (dto != null && dto.IsAvailable)
            {
                cache.Set(cacheKey, dto, TimeSpan.FromMinutes(15));
                return dto;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI interpretation provider failed or timed out for student {StudentId}.", factPackage.StudentId);
        }

        // Clean fallback state — core Academic Intelligence continues working!
        return new AcademicAiSummaryDto(
            Audience: factPackage.Audience,
            Summary: "Yapay zeka değerlendirmesi şu anda kullanılamıyor.",
            Strengths: Array.Empty<string>(),
            ImprovementAreas: Array.Empty<string>(),
            TrendNotes: Array.Empty<string>(),
            SuggestedFocus: Array.Empty<string>(),
            IsAvailable: false,
            GeneratedAt: DateTimeOffset.UtcNow
        );
    }
}
