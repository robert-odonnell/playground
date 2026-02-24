using FamilyChat.Application.Abstractions;
using FamilyChat.Contracts.Conversations;
using FamilyChat.Contracts.Messages;
using FamilyChat.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace FamilyChat.Api.Realtime;

public sealed class SignalRRealtimePublisher(IHubContext<RealtimeHub> hubContext) : IRealtimePublisher
{
    public Task PublishMessageCreatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(GroupNames.Conversation(conversationId))
            .SendAsync(RealtimeEvents.MessageCreated, message, cancellationToken);
    }

    public Task PublishMessageUpdatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(GroupNames.Conversation(conversationId))
            .SendAsync(RealtimeEvents.MessageUpdated, message, cancellationToken);
    }

    public Task PublishMessageDeletedAsync(Guid conversationId, string messageId, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(GroupNames.Conversation(conversationId))
            .SendAsync(RealtimeEvents.MessageDeleted, new MessageDeletedDto { MessageId = messageId }, cancellationToken);
    }

    public Task PublishReactionUpdatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(GroupNames.Conversation(conversationId))
            .SendAsync(RealtimeEvents.MessageReactionUpdated, message, cancellationToken);
    }

    public Task PublishConversationUpdatedAsync(Guid conversationId, ConversationDto conversation, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(GroupNames.Conversation(conversationId))
            .SendAsync(RealtimeEvents.ConversationUpdated, conversation, cancellationToken);
    }

    public Task PublishMemberJoinedAsync(Guid conversationId, ConversationMemberDto member, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(GroupNames.Conversation(conversationId))
            .SendAsync(RealtimeEvents.MemberJoined, member, cancellationToken);
    }

    public Task PublishMemberLeftAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(GroupNames.Conversation(conversationId))
            .SendAsync(RealtimeEvents.MemberLeft, userId, cancellationToken);
    }

    public Task PublishUnreadUpdatedAsync(Guid userId, UnreadUpdatedDto payload, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .User(userId.ToString())
            .SendAsync(RealtimeEvents.UnreadUpdated, payload, cancellationToken);
    }
}
