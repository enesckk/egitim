using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.AI.Services;

public interface IAiLlmProvider
{
    Task<AcademicAiSummaryDto?> InterpretFactsAsync(AcademicFactPackage factPackage, CancellationToken ct = default);
}
