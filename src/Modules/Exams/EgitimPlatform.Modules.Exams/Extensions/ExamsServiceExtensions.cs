using EgitimPlatform.Modules.Exams.Features.CreateAttempt;
using EgitimPlatform.Modules.Exams.Features.CreateExam;
using EgitimPlatform.Modules.Exams.Features.FinalizeAttempt;
using EgitimPlatform.Modules.Exams.Features.GetExamDetail;
using EgitimPlatform.Modules.Exams.Features.GetResultDetail;
using EgitimPlatform.Modules.Exams.Features.GetStudentAnalysis;
using EgitimPlatform.Modules.Exams.Features.ListExams;
using EgitimPlatform.Modules.Exams.Features.ListStudentResults;
using EgitimPlatform.Modules.Exams.Features.UpdateAttempt;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Modules.Exams.Extensions;

public static class ExamsServiceExtensions
{
    public static IServiceCollection AddExamsModule(this IServiceCollection services)
    {
        services.AddScoped<IExamScoringService, ExamScoringService>();
        services.AddScoped<IExamResourceAccess, ExamResourceAccess>();
        services.AddScoped<IExamSummaryQuery, ExamSummaryQuery>();

        services.AddScoped<CreateExamHandler>();
        services.AddScoped<ListExamsHandler>();
        services.AddScoped<GetExamDetailHandler>();

        services.AddScoped<CreateAttemptHandler>();
        services.AddScoped<UpdateAttemptHandler>();
        services.AddScoped<FinalizeAttemptHandler>();
        services.AddScoped<ListStudentResultsHandler>();
        services.AddScoped<GetResultDetailHandler>();
        services.AddScoped<GetStudentAnalysisHandler>();

        return services;
    }
}
