using FamilyChat.Application.Services;
using FamilyChat.Domain.Entities;
using FamilyChat.UnitTests.Helpers;
using FluentAssertions;

namespace FamilyChat.UnitTests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task UpdateReadState_ShouldClampToLatestMessage()
    {
        var db = TestDbContextFactory.Create(nameof(UpdateReadState_ShouldClampToLatestMessage));

        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var latest = DateTime.UtcNow;

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
            CreatedAt = latest
        });

        await db.SaveChangesAsync();

        var service = new NotificationService(db, new TestCurrentUserAccessor(userId));

        await service.UpdateReadStateAsync(conversationId, latest.AddDays(1), CancellationToken.None);

        var state = db.ReadStates.Single();
        state.LastReadAt.Should().Be(latest);
    }
}
