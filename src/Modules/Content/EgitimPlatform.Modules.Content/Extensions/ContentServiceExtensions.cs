using EgitimPlatform.Modules.Content.Features.ManageMaterials;
using EgitimPlatform.Modules.Content.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Modules.Content.Extensions;

public static class ContentServiceExtensions
{
    public static IServiceCollection AddContentModule(this IServiceCollection services)
    {
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IMaterialsHandler, MaterialsHandler>();
        return services;
    }
}
