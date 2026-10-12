using EgitimPlatform.Modules.Questions.Features.ArchiveQuestion;
using EgitimPlatform.Modules.Questions.Features.CreateQuestion;
using EgitimPlatform.Modules.Questions.Features.GetQuestionById;
using EgitimPlatform.Modules.Questions.Features.GetQuestions;
using EgitimPlatform.Modules.Questions.Features.UpdateQuestion;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EgitimPlatform.Modules.Questions.Extensions;

public static class QuestionsServiceExtensions
{
    public static IServiceCollection AddQuestionsModule(this IServiceCollection services)
    {
        services.AddScoped<CreateQuestionHandler>();
        services.AddScoped<GetQuestionByIdHandler>();
        services.AddScoped<GetQuestionsHandler>();
        services.AddScoped<UpdateQuestionHandler>();
        services.AddScoped<ArchiveQuestionHandler>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
