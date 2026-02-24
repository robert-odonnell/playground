using FamilyChat.Contracts.Conversations;
using FamilyChat.Contracts.Messages;
using FamilyChat.Contracts.Realtime;

namespace FamilyChat.Application.Abstractions;

public interface IRealtimePublisher
{
    Task PublishMessageCreatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken);
    Task PublishMessageUpdatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken);
    Task PublishMessageDeletedAsync(Guid conversationId, string messageId, CancellationToken cancellationToken);
    Task PublishReactionUpdatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken);
    Task PublishConversationUpdatedAsync(Guid conversationId, ConversationDto conversation, CancellationToken cancellationToken);
    Task PublishMemberJoinedAsync(Guid conversationId, ConversationMemberDto member, CancellationToken cancellationToken);
    Task PublishMemberLeftAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken);
    Task PublishUnreadUpdatedAsync(Guid userId, UnreadUpdatedDto payload, CancellationToken cancellationToken);
}
