using System.Reflection;
using EgitimPlatform.Modules.Notebook.Features.ManageNotebook;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Modules.Notebook.Extensions;

public static class NotebookServiceExtensions
{
    public static IServiceCollection AddNotebookModule(this IServiceCollection services)
    {
        services.AddScoped<NotebookHandler>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
