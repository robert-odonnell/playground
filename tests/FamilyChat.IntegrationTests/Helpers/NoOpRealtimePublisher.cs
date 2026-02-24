using FamilyChat.Application.Abstractions;
using FamilyChat.Contracts.Conversations;
using FamilyChat.Contracts.Messages;
using FamilyChat.Contracts.Realtime;

namespace FamilyChat.IntegrationTests.Helpers;

public sealed class NoOpRealtimePublisher : IRealtimePublisher
{
    public Task PublishMessageCreatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublishMessageUpdatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublishMessageDeletedAsync(Guid conversationId, string messageId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublishReactionUpdatedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublishConversationUpdatedAsync(Guid conversationId, ConversationDto conversation, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublishMemberJoinedAsync(Guid conversationId, ConversationMemberDto member, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublishMemberLeftAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublishUnreadUpdatedAsync(Guid userId, UnreadUpdatedDto payload, CancellationToken cancellationToken) => Task.CompletedTask;
}
