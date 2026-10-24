using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Communication.Features;
using EgitimPlatform.Modules.Communication.Features.ManageMessages;
using EgitimPlatform.Modules.Communication.Jobs;
using EgitimPlatform.Modules.Communication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EgitimPlatform.Modules.Communication.Extensions;

public static class CommunicationServiceExtensions
{
    public static IServiceCollection AddCommunicationModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ListNotificationsHandler>();
        services.AddScoped<GetUnreadNotificationCountHandler>();
        services.AddScoped<MarkNotificationReadHandler>();
        services.AddScoped<MarkAllNotificationsReadHandler>();
        services.AddScoped<Features.ManagePreferences.INotificationPreferencesHandler, Features.ManagePreferences.NotificationPreferencesHandler>();
        services.AddScoped<DueSoonNotificationJob>();
        services.AddScoped<MessagesHandler>();

        return services;
    }
}
