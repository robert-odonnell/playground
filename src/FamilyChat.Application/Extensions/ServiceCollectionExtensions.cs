using FamilyChat.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyChat.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ConversationService>();
        services.AddScoped<MessageService>();
        services.AddScoped<ReactionService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<SearchService>();
        services.AddScoped<AdminService>();
        return services;
    }
}
