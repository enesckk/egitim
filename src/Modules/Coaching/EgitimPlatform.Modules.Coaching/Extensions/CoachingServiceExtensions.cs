using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EgitimPlatform.Modules.Coaching.Extensions;

public static class CoachingServiceExtensions
{
    public static IServiceCollection AddCoachingModule(this IServiceCollection services)
    {
        services.AddScoped<AssignCoachHandler>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
