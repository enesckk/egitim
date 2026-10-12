using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.AI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Modules.AI.Extensions;

public static class AiModuleServiceExtensions
{
    public static IServiceCollection AddAiModule(this IServiceCollection services)
    {
        services.AddScoped<IAiLlmProvider, DefaultAcademicInterpretationProvider>();
        services.AddScoped<IAcademicInterpretationService, AcademicInterpretationService>();
        services.AddScoped<EgitimPlatform.Modules.AI.Features.GetStudentAcademicAiSummaryHandler>();
        return services;
    }
}
