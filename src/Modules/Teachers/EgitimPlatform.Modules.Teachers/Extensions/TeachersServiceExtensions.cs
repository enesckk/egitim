using EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
using Microsoft.Extensions.DependencyInjection;
namespace EgitimPlatform.Modules.Teachers.Extensions;
public static class TeachersServiceExtensions
{
    public static IServiceCollection AddTeachersModule(this IServiceCollection services) { services.AddScoped<TeacherHandler>(); return services; }
}
