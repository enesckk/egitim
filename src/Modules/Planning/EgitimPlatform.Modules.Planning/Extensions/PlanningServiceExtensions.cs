using EgitimPlatform.Modules.Planning.Features;
using EgitimPlatform.Modules.Planning.Features.GetInstitutionOverview;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Modules.Planning.Extensions;

public static class PlanningServiceExtensions
{
    public static IServiceCollection AddPlanningModule(this IServiceCollection services)
    {
        services.AddScoped<AcceptAcademicStudyPlanHandler>();
        services.AddScoped<GetActiveStudyPlanHandler>();
        services.AddScoped<GetStudyPlanHandler>();
        services.AddScoped<ListStudyPlansHandler>();
        services.AddScoped<UpdateTaskCompletionHandler>();
        services.AddScoped<CancelStudyPlanHandler>();
        services.AddScoped<GetInstitutionOverviewHandler>();

        services.AddScoped<CreateCoachTaskHandler>();
        services.AddScoped<ListCoachTasksHandler>();
        services.AddScoped<UpdateCoachTaskHandler>();
        services.AddScoped<CancelCoachTaskHandler>();
        services.AddScoped<UpdateCoachTaskCompletionHandler>();

        return services;
    }
}
