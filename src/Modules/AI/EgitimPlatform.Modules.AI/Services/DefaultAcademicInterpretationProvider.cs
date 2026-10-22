using EgitimPlatform.BuildingBlocks.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EgitimPlatform.Modules.AI.Services;

public class DefaultAcademicInterpretationProvider(IConfiguration configuration) : IAiLlmProvider
{
    public Task<AcademicAiSummaryDto?> InterpretFactsAsync(AcademicFactPackage factPackage, CancellationToken ct = default)
    {
        var rawVal = configuration["AiSettings:Enabled"];
        var enabled = string.IsNullOrWhiteSpace(rawVal) || (bool.TryParse(rawVal, out var b) ? b : true);
        if (!enabled)
        {
            return Task.FromResult<AcademicAiSummaryDto?>(null);
        }

        var audience = (factPackage.Audience ?? "student").ToLowerInvariant();
        var questionStyles = factPackage.QuestionStyles ?? [];

        var strengths = factPackage.Classifications
            .Where(c => c.ClassificationState.Equals("Strong", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.TopicName)
            .Concat(questionStyles
                .Where(s => s.ClassificationState.Equals("Strong", StringComparison.OrdinalIgnoreCase))
                .Select(s => $"{s.Name} (Soru Tarzı)"))
            .ToList();

        var improvementAreas = factPackage.Classifications
            .Where(c => c.ClassificationState.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
                        c.ClassificationState.Equals("Weak", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.TopicName)
            .Concat(questionStyles
                .Where(s => s.ClassificationState.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
                            s.ClassificationState.Equals("Weak", StringComparison.OrdinalIgnoreCase))
                .Select(s => $"{s.Name} (Soru Tarzı)"))
            .ToList();

        var trendNotes = factPackage.Trends
            .Select(t => $"{t.SubjectName}: {t.Message}")
            .Concat(questionStyles
                .Where(s => s.TrendDirection != null &&
                            (s.TrendDirection.Equals("Increasing", StringComparison.OrdinalIgnoreCase) ||
                             s.TrendDirection.Equals("Decreasing", StringComparison.OrdinalIgnoreCase)))
                .Select(s => $"{s.Name} (Soru Tarzı): {s.TrendDirection}"))
            .ToList();

        var suggestedFocus = factPackage.Recommendations
            .Select(r => r.Title)
            .ToList();

        string summary;
        if (strengths.Count == 0 && improvementAreas.Count == 0 && trendNotes.Count == 0)
        {
            summary = audience switch
            {
                "coach" => "Öğrenci için henüz yeterli akademik analiz verisi bulunmamaktadır.",
                "parent" => "Öğrencimizin henüz yeterli sınav sonucu bulunmamaktadır.",
                _ => "Henüz yeterli akademik analiz verisi bulunmamaktadır."
            };
        }
        else
        {
            summary = audience switch
            {
                "coach" => $"Öğrencinin performans özeti: {improvementAreas.Count} zayıf/kritik alan, {strengths.Count} güçlü konu tespit edildi. Çalışma planı verimliliği: %{(factPackage.PlanSummary?.UtilizationRate * 100 ?? 0):F0}.",
                "parent" => $"Öğrencimizin genel akademik durumu: {strengths.Count} konuda başarılı, {improvementAreas.Count} konuda desteğe ihtiyaç duyuyor.",
                _ => $"Akademik durum analizi: {strengths.Count} konuda güçlü performans gösteriyorsun. {improvementAreas.Count} konuda çalışmaya odaklanmalısın."
            };
        }

        var dto = new AcademicAiSummaryDto(
            Audience: audience,
            Summary: summary,
            Strengths: strengths,
            ImprovementAreas: improvementAreas,
            TrendNotes: trendNotes,
            SuggestedFocus: suggestedFocus,
            IsAvailable: true,
            GeneratedAt: DateTimeOffset.UtcNow
        );

        return Task.FromResult<AcademicAiSummaryDto?>(dto);
    }
}
