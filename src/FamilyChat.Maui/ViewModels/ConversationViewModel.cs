using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyChat.ClientSdk.Api;
using FamilyChat.ClientSdk.Realtime;
using FamilyChat.Contracts.Messages;
using FamilyChat.Contracts.Notifications;
using FamilyChat.Contracts.Realtime;
using FamilyChat.Maui.Services;

namespace FamilyChat.Maui.ViewModels;

public partial class ConversationViewModel(
    FamilyChatApiClient apiClient,
    FamilyChatRealtimeClient realtimeClient,
    AppState appState) : ObservableObject
{
    private string? _cursor;

    public ObservableCollection<MessageDto> Messages { get; } = [];

    [ObservableProperty]
    private Guid conversationId;

    [ObservableProperty]
    private string draft = string.Empty;

    [RelayCommand]
    private async Task InitializeAsync(Guid conversationId)
    {
        ConversationId = conversationId;
        appState.ActiveConversationId = conversationId;

        await realtimeClient.JoinConversationAsync(conversationId);
        realtimeClient.OnMessageCreated(OnMessageCreated);
        realtimeClient.OnMessageUpdated(OnMessageUpdated);
        realtimeClient.OnMessageDeleted(OnMessageDeleted);
        realtimeClient.OnMessageReactionUpdated(OnMessageUpdated);

        await LoadLatestAsync();
    }

    [RelayCommand]
    private async Task LoadLatestAsync()
    {
        var page = await apiClient.GetMessagesAsync(ConversationId, null, 50, CancellationToken.None);
        _cursor = page.NextCursor;

        Messages.Clear();
        foreach (var message in page.Items)
        {
            Messages.Add(message);
        }

        if (Messages.Count > 0)
        {
            var lastVisible = Messages.Last().CreatedAt;
            await apiClient.UpdateReadStateAsync(ConversationId, new UpdateReadStateRequestDto
            {
                LastReadAt = lastVisible
            }, CancellationToken.None);
        }
    }

    [RelayCommand]
    private async Task LoadOlderAsync()
    {
        if (string.IsNullOrWhiteSpace(_cursor))
        {
            return;
        }

        var page = await apiClient.GetMessagesAsync(ConversationId, _cursor, 50, CancellationToken.None);
        _cursor = page.NextCursor;

        foreach (var message in page.Items)
        {
            Messages.Insert(0, message);
        }
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        var body = Draft.Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        Draft = string.Empty;

        var message = await apiClient.CreateMessageAsync(ConversationId, new CreateMessageRequestDto
        {
            Body = body
        }, CancellationToken.None);

        Messages.Add(message);
    }

    [RelayCommand]
    private async Task ToggleReactionAsync(MessageDto message)
    {
        if (message is null)
        {
            return;
        }

        var updated = await apiClient.ToggleReactionAsync(message.Id, "👍", CancellationToken.None);
        Replace(updated);
    }

    [RelayCommand]
    private async Task DeleteMessageAsync(MessageDto message)
    {
        if (message is null)
        {
            return;
        }

        await apiClient.DeleteMessageAsync(message.Id, CancellationToken.None);
    }

    private void OnMessageCreated(MessageDto message)
    {
        if (message.ConversationId != ConversationId)
        {
            return;
        }

        if (Messages.All(item => item.Id != message.Id))
        {
            Messages.Add(message);
        }
    }

    private void OnMessageUpdated(MessageDto message)
    {
        if (message.ConversationId != ConversationId)
        {
            return;
        }

        Replace(message);
    }

    private void OnMessageDeleted(MessageDeletedDto payload)
    {
        var target = Messages.FirstOrDefault(item => item.Id == payload.MessageId);
        if (target is null)
        {
            return;
        }

        var index = Messages.IndexOf(target);
        target.Body = "";
        target.DeletedAt = DateTime.UtcNow;
        Messages[index] = target;
    }

    private void Replace(MessageDto message)
    {
        var existing = Messages.FirstOrDefault(item => item.Id == message.Id);
        if (existing is null)
        {
            Messages.Add(message);
            return;
        }

        var index = Messages.IndexOf(existing);
        Messages[index] = message;
    }
}
