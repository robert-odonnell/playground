using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyChat.ClientSdk.Api;
using FamilyChat.Contracts.Auth;
using FamilyChat.Maui.Services;

namespace FamilyChat.Maui.ViewModels;

public partial class LoginViewModel(
    FamilyChatApiClient apiClient,
    AppState appState,
    InstallationService installationService,
    IServiceProvider serviceProvider) : ObservableObject
{
    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string token = string.Empty;

    [ObservableProperty]
    private string status = "Enter your email to request a magic link.";

    [RelayCommand]
    private async Task RequestLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            Status = "Email is required.";
            return;
        }

        await apiClient.RequestMagicLinkAsync(new MagicLinkRequestDto
        {
            Email = Email,
            InstallationId = installationService.GetInstallationId(),
            Platform = DeviceInfo.Current.Platform.ToString()
        }, CancellationToken.None);

        Status = "Magic link requested. Paste token from email to continue.";
    }

    [RelayCommand]
    private async Task VerifyTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Token))
        {
            Status = "Email and token are required.";
            return;
        }

        var auth = await apiClient.VerifyMagicLinkAsync(new MagicLinkVerifyDto
        {
            Email = Email,
            Token = Token,
            InstallationId = installationService.GetInstallationId(),
            Platform = DeviceInfo.Current.Platform.ToString()
        }, CancellationToken.None);

        appState.AccessToken = auth.AccessToken;
        appState.RefreshToken = auth.RefreshToken;
        appState.CurrentUser = auth.User;
        apiClient.AccessToken = auth.AccessToken;

        Application.Current!.MainPage = serviceProvider.GetRequiredService<AppShell>();
    }
}
