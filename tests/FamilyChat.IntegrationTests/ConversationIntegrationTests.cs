using FamilyChat.Application.Services;
using FamilyChat.Contracts.Conversations;
using FamilyChat.Domain.Entities;
using FamilyChat.Domain.Enums;
using FamilyChat.IntegrationTests.Helpers;
using FluentAssertions;

namespace FamilyChat.IntegrationTests;

public sealed class ConversationIntegrationTests
{
    [Fact]
    public async Task CreateDm_ShouldReturnExistingConversationForSamePair()
    {
        var db = TestDbContextFactory.Create(nameof(CreateDm_ShouldReturnExistingConversationForSamePair));
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = me, Email = "me@test.local", DisplayName = "Me", CreatedAt = DateTime.UtcNow },
            new User { Id = other, Email = "other@test.local", DisplayName = "Other", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor(me);
        var notifications = new NotificationService(db, currentUser);
        var service = new ConversationService(db, currentUser, notifications, new NoOpRealtimePublisher());

        var first = await service.CreateOrGetDmAsync(new CreateDmRequestDto { OtherUserId = other }, CancellationToken.None);
        var second = await service.CreateOrGetDmAsync(new CreateDmRequestDto { OtherUserId = other }, CancellationToken.None);

        first.Id.Should().Be(second.Id);
        db.Conversations.Count(conversation => conversation.Type == ConversationType.DM).Should().Be(1);
    }
}
