using FamilyChat.Contracts.Conversations;
using FamilyChat.Contracts.Messages;
using FamilyChat.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

namespace FamilyChat.ClientSdk.Realtime;

public sealed class FamilyChatRealtimeClient
{
    private readonly HubConnection _connection;

    public FamilyChatRealtimeClient(string realtimeUrl, Func<Task<string?>> accessTokenProvider)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(realtimeUrl, options =>
            {
                options.AccessTokenProvider = accessTokenProvider;
            })
            .WithAutomaticReconnect()
            .Build();
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => _connection.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) => _connection.StopAsync(cancellationToken);

    public Task JoinConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return _connection.InvokeAsync("JoinConversation", conversationId, cancellationToken);
    }

    public Task LeaveConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return _connection.InvokeAsync("LeaveConversation", conversationId, cancellationToken);
    }

    public IDisposable OnMessageCreated(Action<MessageDto> handler)
    {
        return _connection.On(RealtimeEvents.MessageCreated, handler);
    }

    public IDisposable OnMessageUpdated(Action<MessageDto> handler)
    {
        return _connection.On(RealtimeEvents.MessageUpdated, handler);
    }

    public IDisposable OnMessageDeleted(Action<MessageDeletedDto> handler)
    {
        return _connection.On(RealtimeEvents.MessageDeleted, handler);
    }

    public IDisposable OnMessageReactionUpdated(Action<MessageDto> handler)
    {
        return _connection.On(RealtimeEvents.MessageReactionUpdated, handler);
    }

    public IDisposable OnConversationUpdated(Action<ConversationDto> handler)
    {
        return _connection.On(RealtimeEvents.ConversationUpdated, handler);
    }

    public IDisposable OnMemberJoined(Action<ConversationMemberDto> handler)
    {
        return _connection.On(RealtimeEvents.MemberJoined, handler);
    }

    public IDisposable OnMemberLeft(Action<Guid> handler)
    {
        return _connection.On<Guid>(RealtimeEvents.MemberLeft, handler);
    }

    public IDisposable OnUnreadUpdated(Action<UnreadUpdatedDto> handler)
    {
        return _connection.On(RealtimeEvents.UnreadUpdated, handler);
    }
}
