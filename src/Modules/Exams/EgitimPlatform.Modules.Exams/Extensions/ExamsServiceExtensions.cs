using EgitimPlatform.Modules.Exams.Authorization;
using EgitimPlatform.Modules.Exams.Features;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.BuildingBlocks.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EgitimPlatform.Modules.Exams.Extensions;

public static class ExamsServiceExtensions
{
    public static IServiceCollection AddExamsModule(this IServiceCollection services)
    {
        services.AddScoped<IExamResourceAccessService, ExamResourceAccessService>();
        services.AddScoped<IExamTaxonomyValidator, ExamTaxonomyValidator>();
        services.AddSingleton<IExamScoringService, ExamScoringService>();
        services.AddSingleton<ExamAttemptInvariantService>();
        services.AddSingleton<ExamLifecycleService>();
        services.AddScoped<ExamHandler>();
        services.AddScoped<ExamAttemptHandler>();
        services.AddScoped<ExamResultQueryHandler>();
        services.AddScoped<IStudentExamSummaryQuery, StudentExamSummaryQuery>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
