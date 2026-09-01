using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Modules.Students.Features.GetStudent;
using EgitimPlatform.Modules.Students.Features.ListStudents;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EgitimPlatform.Modules.Students.Extensions;

public static class StudentsServiceExtensions
{
    public static IServiceCollection AddStudentsModule(this IServiceCollection services)
    {
        services.AddScoped<CreateStudentHandler>();
        services.AddScoped<GetStudentHandler>();
        services.AddScoped<ListStudentsHandler>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
