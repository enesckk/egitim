using EgitimPlatform.Modules.Reporting.Features.InstitutionalReports;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Modules.Reporting.Extensions;

public static class ReportingServiceExtensions
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<IInstitutionalReportHandler, InstitutionalReportHandler>();
        return services;
    }
}
