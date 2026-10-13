using EgitimPlatform.Modules.Exams.Features.CreateAttempt;
using EgitimPlatform.Modules.Exams.Features.CreateExam;
using EgitimPlatform.Modules.Exams.Features.AddExamQuestion;
using EgitimPlatform.Modules.Exams.Features.ListExamQuestions;
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
        services.AddScoped<IRawExamAttemptFinalizer, RawExamAttemptFinalizer>();
        services.AddScoped<IExamResourceAccess, ExamResourceAccess>();
        services.AddScoped<IExamSummaryQuery, ExamSummaryQuery>();
        services.AddScoped<IQuestionUsageChecker, QuestionUsageChecker>();

        services.AddScoped<CreateExamHandler>();
        services.AddScoped<ListExamsHandler>();
        services.AddScoped<GetExamDetailHandler>();
        services.AddScoped<AddExamQuestionHandler>();
        services.AddScoped<ListExamQuestionsHandler>();

        services.AddScoped<CreateAttemptHandler>();
        services.AddScoped<UpdateAttemptHandler>();
        services.AddScoped<FinalizeAttemptHandler>();
        services.AddScoped<ListStudentResultsHandler>();
        services.AddScoped<GetResultDetailHandler>();
        services.AddScoped<GetStudentAnalysisHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.SaveAnswers.SaveAnswersHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.GetAttemptQuestionResults.GetAttemptQuestionResultsHandler>();

        services.AddScoped<IAggregateExamAttemptCreator, AggregateExamAttemptCreator>();

        services.AddScoped<IImportFileValidator, ImportFileValidator>();
        services.AddScoped<IExamResultTemplateGenerator, ExamResultTemplateGenerator>();
        services.AddScoped<IExamResultImportPreviewEngine, ExamResultImportPreviewEngine>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.ExamResultImport.GenerateImportTemplateHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.ExamResultImport.PreviewResultImportHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.ExamResultImport.CommitResultImportHandler>();

        services.AddScoped<EgitimPlatform.Modules.Exams.Features.CreateManualResult.CreateSingleManualResultHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.CreateManualResult.CreateBulkManualResultHandler>();

        services.AddScoped<EgitimPlatform.Modules.Exams.Features.AcademicIntelligence.GetStudentAcademicIntelligenceHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.AcademicIntelligence.GetStudentAcademicTrendHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.AcademicIntelligence.GetStudentAcademicClassificationHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.AcademicIntelligence.GetStudentKnowledgeMapHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.AcademicIntelligence.GetStudentAcademicRecommendationHandler>();
        services.AddScoped<EgitimPlatform.Modules.Exams.Features.AcademicIntelligence.GetStudentAcademicStudyPlanHandler>();

        return services;
    }
}
