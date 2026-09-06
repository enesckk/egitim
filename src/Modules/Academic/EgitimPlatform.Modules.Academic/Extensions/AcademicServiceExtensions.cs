using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Academic.Features;
using EgitimPlatform.Modules.Academic.Seeding;
using Microsoft.Extensions.DependencyInjection;
namespace EgitimPlatform.Modules.Academic.Extensions;
public static class AcademicServiceExtensions
{
    public static IServiceCollection AddAcademicModule(this IServiceCollection services)
    {
        services.AddScoped<TaxonomyHandler>();
        services.AddScoped<IAcademicCatalog>(sp => sp.GetRequiredService<TaxonomyHandler>());
        services.AddScoped<TaxonomySeeder>();
        return services;
    }
}
