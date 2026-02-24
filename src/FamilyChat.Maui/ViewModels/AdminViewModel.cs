using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyChat.ClientSdk.Api;
using FamilyChat.Contracts.Admin;
using FamilyChat.Contracts.Conversations;

namespace FamilyChat.Maui.ViewModels;

public partial class AdminViewModel(FamilyChatApiClient apiClient) : ObservableObject
{
    public ObservableCollection<AdminUserDto> Users { get; } = [];
    public ObservableCollection<AdminChannelDto> Channels { get; } = [];

    [ObservableProperty]
    private string newUserEmail = string.Empty;

    [ObservableProperty]
    private string newUserDisplayName = string.Empty;

    [ObservableProperty]
    private string newChannelName = string.Empty;

    [ObservableProperty]
    private bool newChannelPrivate;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var users = await apiClient.GetAdminUsersAsync(CancellationToken.None);
        var channels = await apiClient.GetAdminChannelsAsync(CancellationToken.None);

        Users.Clear();
        foreach (var user in users)
        {
            Users.Add(user);
        }

        Channels.Clear();
        foreach (var channel in channels)
        {
            Channels.Add(channel);
        }
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUserEmail) || string.IsNullOrWhiteSpace(NewUserDisplayName))
        {
            return;
        }

        var created = await apiClient.AddAdminUserAsync(new AdminCreateUserRequestDto
        {
            Email = NewUserEmail,
            DisplayName = NewUserDisplayName,
            IsAdmin = false
        }, CancellationToken.None);

        Users.Add(created);
        NewUserEmail = string.Empty;
        NewUserDisplayName = string.Empty;
    }

    [RelayCommand]
    private async Task ToggleUserDisabledAsync(AdminUserDto user)
    {
        if (user is null)
        {
            return;
        }

        var updated = await apiClient.UpdateAdminUserAsync(user.Id, new AdminUpdateUserRequestDto
        {
            IsDisabled = !user.IsDisabled
        }, CancellationToken.None);

        var index = Users.IndexOf(user);
        Users[index] = updated;
    }

    [RelayCommand]
    private async Task ToggleUserAdminAsync(AdminUserDto user)
    {
        if (user is null)
        {
            return;
        }

        var updated = await apiClient.UpdateAdminUserAsync(user.Id, new AdminUpdateUserRequestDto
        {
            IsAdmin = !user.IsAdmin
        }, CancellationToken.None);

        var index = Users.IndexOf(user);
        Users[index] = updated;
    }

    [RelayCommand]
    private async Task AddChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(NewChannelName))
        {
            return;
        }

        var created = await apiClient.AddAdminChannelAsync(new CreateChannelRequestDto
        {
            Name = NewChannelName,
            IsPrivate = NewChannelPrivate
        }, CancellationToken.None);

        Channels.Add(created);
        NewChannelName = string.Empty;
        NewChannelPrivate = false;
    }
}
