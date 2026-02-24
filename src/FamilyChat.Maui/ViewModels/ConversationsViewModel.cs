using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyChat.ClientSdk.Api;
using FamilyChat.ClientSdk.Realtime;
using FamilyChat.Contracts.Conversations;
using FamilyChat.Contracts.Realtime;

namespace FamilyChat.Maui.ViewModels;

public partial class ConversationsViewModel(
    FamilyChatApiClient apiClient,
    FamilyChatRealtimeClient realtimeClient) : ObservableObject
{
    private bool _realtimeStarted;

    public ObservableCollection<ConversationDto> Conversations { get; } = [];

    [ObservableProperty]
    private bool isRefreshing;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;

        var response = await apiClient.GetConversationsAsync(CancellationToken.None);

        Conversations.Clear();
        foreach (var conversation in response)
        {
            Conversations.Add(conversation);
        }

        if (!_realtimeStarted)
        {
            await realtimeClient.StartAsync();
            realtimeClient.OnUnreadUpdated(OnUnreadUpdated);
            _realtimeStarted = true;
        }

        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task OpenConversationAsync(ConversationDto conversation)
    {
        if (conversation is null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"conversation?conversationId={conversation.Id}");
    }

    private void OnUnreadUpdated(UnreadUpdatedDto payload)
    {
        var conversation = Conversations.FirstOrDefault(item => item.Id == payload.ConversationId);
        if (conversation is null)
        {
            return;
        }

        var index = Conversations.IndexOf(conversation);
        conversation.UnreadCount = payload.UnreadCount;
        Conversations[index] = conversation;
    }
}
