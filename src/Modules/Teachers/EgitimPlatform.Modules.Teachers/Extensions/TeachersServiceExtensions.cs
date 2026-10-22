using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Teachers.Features.ManageClassrooms;
using EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
using EgitimPlatform.Modules.Teachers.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EgitimPlatform.Modules.Teachers.Extensions;

public static class TeachersServiceExtensions
{
    public static IServiceCollection AddTeachersModule(this IServiceCollection services)
    {
        services.AddScoped<TeacherHandler>();
        services.AddScoped<ClassroomHandler>();
        services.AddScoped<ITeacherStudentQuery, TeacherStudentQuery>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
