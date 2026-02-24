using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyChat.ClientSdk.Api;
using FamilyChat.Contracts.Notifications;

namespace FamilyChat.Maui.ViewModels;

public partial class SettingsViewModel(FamilyChatApiClient apiClient) : ObservableObject
{
    [ObservableProperty]
    private bool inAppToastsEnabled;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var response = await apiClient.GetNotificationPreferencesAsync(CancellationToken.None);
        InAppToastsEnabled = response.InAppToastsEnabled;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await apiClient.UpdateNotificationPreferencesAsync(new UserNotificationPreferenceDto
        {
            InAppToastsEnabled = InAppToastsEnabled
        }, CancellationToken.None);
    }
}
