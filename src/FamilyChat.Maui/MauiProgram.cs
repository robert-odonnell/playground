using CommunityToolkit.Maui;
using FamilyChat.ClientSdk.Api;
using FamilyChat.ClientSdk.Realtime;
using FamilyChat.Maui.Pages;
using FamilyChat.Maui.Services;
using FamilyChat.Maui.ViewModels;
using Microsoft.Extensions.Logging;

namespace FamilyChat.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<InstallationService>();

        builder.Services.AddHttpClient<FamilyChatApiClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5000/");
        });

        builder.Services.AddSingleton<FamilyChatRealtimeClient>(provider =>
        {
            var state = provider.GetRequiredService<AppState>();
            return new FamilyChatRealtimeClient("http://localhost:5000/realtime", () =>
            {
                if (string.IsNullOrWhiteSpace(state.AccessToken))
                {
                    return Task.FromResult(string.Empty);
                }

                return Task.FromResult(state.AccessToken!);
            });
        });

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ConversationsViewModel>();
        builder.Services.AddTransient<ConversationViewModel>();
        builder.Services.AddTransient<SearchViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<AdminViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ConversationsPage>();
        builder.Services.AddTransient<ConversationPage>();
        builder.Services.AddTransient<SearchPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AdminPage>();
        builder.Services.AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
