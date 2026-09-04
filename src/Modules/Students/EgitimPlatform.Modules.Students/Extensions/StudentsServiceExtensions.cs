using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Modules.Students.Features.CreateStudentGoal;
using EgitimPlatform.Modules.Students.Features.DeactivateStudentGoal;
using EgitimPlatform.Modules.Students.Features.GetStudent;
using EgitimPlatform.Modules.Students.Features.GetStudentGoal;
using EgitimPlatform.Modules.Students.Features.GetStudentGoalHistory;
using EgitimPlatform.Modules.Students.Features.ListStudentGoals;
using EgitimPlatform.Modules.Students.Features.ListStudents;
using EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;
using EgitimPlatform.Modules.Students.Features.UpdateStudentGoal;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EgitimPlatform.Modules.Students.Extensions;

public static class StudentsServiceExtensions
{
    public static IServiceCollection AddStudentsModule(this IServiceCollection services)
    {
        // Sprint 1 handlers
        services.AddScoped<CreateStudentHandler>();
        services.AddScoped<GetStudentHandler>();
        services.AddScoped<ListStudentsHandler>();

        // Sprint 2 handlers
        services.AddScoped<UpdateAcademicProfileHandler>();
        services.AddScoped<CreateStudentGoalHandler>();
        services.AddScoped<UpdateStudentGoalHandler>();
        services.AddScoped<DeactivateStudentGoalHandler>();
        services.AddScoped<GetStudentGoalHandler>();
        services.AddScoped<ListStudentGoalsHandler>();
        services.AddScoped<GetStudentGoalHistoryHandler>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
