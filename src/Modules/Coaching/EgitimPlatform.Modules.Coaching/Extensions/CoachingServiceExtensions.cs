using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using EgitimPlatform.Modules.Coaching.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EgitimPlatform.Modules.Coaching.Extensions;

public static class CoachingServiceExtensions
{
    public static IServiceCollection AddCoachingModule(this IServiceCollection services)
    {
        services.AddScoped<AssignCoachHandler>();
        services.AddScoped<ICoachStudentQuery, CoachStudentQuery>();
        services.AddScoped<IStudentCoachAssigner, StudentCoachAssigner>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
