using FamilyChat.Application.Services;
using FamilyChat.Domain.Entities;
using FamilyChat.UnitTests.Helpers;
using FluentAssertions;

namespace FamilyChat.UnitTests;

public sealed class ReactionServiceTests
{
    [Fact]
    public async Task ToggleReaction_ShouldAddThenRemoveCurrentUser()
    {
        var db = TestDbContextFactory.Create(nameof(ToggleReaction_ShouldAddThenRemoveCurrentUser));

        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = userId,
            Email = "user@test.local",
            DisplayName = "User",
            CreatedAt = DateTime.UtcNow
        });

        db.Conversations.Add(new Conversation
        {
            Id = conversationId,
            Type = Domain.Enums.ConversationType.Channel,
            Name = "general",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        db.ConversationMembers.Add(new ConversationMember
        {
            ConversationId = conversationId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });

        db.Messages.Add(new Message
        {
            Id = "01HFYK4PZQ7QEVJDH85G1D1RZW",
            ConversationId = conversationId,
            SenderId = userId,
            Body = "hello",
            CreatedAt = DateTime.UtcNow,
            ReactionsJson = "{}"
        });

        await db.SaveChangesAsync();

        var service = new ReactionService(db, new TestCurrentUserAccessor(userId), new NoOpRealtimePublisher());

        var afterAdd = await service.ToggleReactionAsync("01HFYK4PZQ7QEVJDH85G1D1RZW", "🔥", CancellationToken.None);
        afterAdd.Reactions.Should().ContainKey("🔥");
        afterAdd.Reactions["🔥"].Should().Contain(userId);

        var afterRemove = await service.ToggleReactionAsync("01HFYK4PZQ7QEVJDH85G1D1RZW", "🔥", CancellationToken.None);
        afterRemove.Reactions.Should().NotContainKey("🔥");
    }
}
