using FamilyChat.Application.Exceptions;
using FamilyChat.Application.Services;
using FamilyChat.Domain.Entities;
using FamilyChat.UnitTests.Helpers;
using FluentAssertions;

namespace FamilyChat.UnitTests;

public sealed class ConversationPermissionsTests
{
    [Fact]
    public async Task JoinConversation_ShouldRejectPrivateChannel()
    {
        var db = TestDbContextFactory.Create(nameof(JoinConversation_ShouldRejectPrivateChannel));

        var creatorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = creatorId, Email = "creator@test.local", DisplayName = "Creator", CreatedAt = DateTime.UtcNow },
            new User { Id = userId, Email = "user@test.local", DisplayName = "User", CreatedAt = DateTime.UtcNow });

        db.Conversations.Add(new Conversation
        {
            Id = conversationId,
            Type = Domain.Enums.ConversationType.Channel,
            Name = "private",
            IsPrivate = true,
            CreatedByUserId = creatorId,
            CreatedAt = DateTime.UtcNow
        });

        db.ConversationMembers.Add(new ConversationMember
        {
            ConversationId = conversationId,
            UserId = creatorId,
            JoinedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var notificationService = new NotificationService(db, new TestCurrentUserAccessor(userId));
        var service = new ConversationService(
            db,
            new TestCurrentUserAccessor(userId),
            notificationService,
            new NoOpRealtimePublisher());

        var act = () => service.JoinConversationAsync(conversationId, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
