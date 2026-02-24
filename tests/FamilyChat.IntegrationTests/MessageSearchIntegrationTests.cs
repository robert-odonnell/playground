using FamilyChat.Application.Services;
using FamilyChat.Contracts.Messages;
using FamilyChat.Domain.Entities;
using FamilyChat.Domain.Enums;
using FamilyChat.IntegrationTests.Helpers;
using FluentAssertions;

namespace FamilyChat.IntegrationTests;

public sealed class MessageSearchIntegrationTests
{
    [Fact]
    public async Task MessageFlow_ShouldSupportMentionsCursorAndSearchScoping()
    {
        var db = TestDbContextFactory.Create(nameof(MessageFlow_ShouldSupportMentionsCursorAndSearchScoping));

        var me = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = me, Email = "me@test.local", DisplayName = "Me", CreatedAt = DateTime.UtcNow },
            new User { Id = bob, Email = "bob@test.local", DisplayName = "Bob", CreatedAt = DateTime.UtcNow },
            new User { Id = outsider, Email = "outsider@test.local", DisplayName = "Outsider", CreatedAt = DateTime.UtcNow });

        db.Conversations.Add(new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Channel,
            Name = "family",
            IsPrivate = false,
            CreatedByUserId = me,
            CreatedAt = DateTime.UtcNow
        });

        db.ConversationMembers.AddRange(
            new ConversationMember { ConversationId = conversationId, UserId = me, JoinedAt = DateTime.UtcNow },
            new ConversationMember { ConversationId = conversationId, UserId = bob, JoinedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var notifications = new NotificationService(db, new TestCurrentUserAccessor(me));
        var messageService = new MessageService(
            db,
            new TestCurrentUserAccessor(me),
            new TestUlidGenerator(),
            notifications,
            new NoOpRealtimePublisher());

        var searchService = new SearchService(db, new TestCurrentUserAccessor(me));

        await messageService.CreateMessageAsync(conversationId, new CreateMessageRequestDto
        {
            Body = "hello @Bob",
            Attachments =
            [
                new AttachmentInputDto
                {
                    Provider = Contracts.Enums.AttachmentProvider.GoogleDrive,
                    FileName = "photo.jpg",
                    ShareUrl = "https://drive.test/photo"
                }
            ]
        }, CancellationToken.None);

        await messageService.CreateMessageAsync(conversationId, new CreateMessageRequestDto
        {
            Body = "follow up"
        }, CancellationToken.None);

        var pageOne = await messageService.GetMessagesAsync(conversationId, null, 1, CancellationToken.None);
        pageOne.Items.Should().HaveCount(1);
        pageOne.NextCursor.Should().NotBeNull();

        var pageTwo = await messageService.GetMessagesAsync(conversationId, pageOne.NextCursor, 10, CancellationToken.None);
        pageTwo.Items.Should().NotBeEmpty();
        pageTwo.Items.SelectMany(message => message.MentionUserIds).Should().Contain(bob);

        var search = await searchService.SearchAsync("photo", conversationId, 20, CancellationToken.None);
        search.Hits.Any(hit => hit.Kind == Contracts.Search.SearchHitKind.Attachment).Should().BeTrue();

        var outsiderSearchService = new SearchService(db, new TestCurrentUserAccessor(outsider));
        var act = () => outsiderSearchService.SearchAsync("photo", conversationId, 20, CancellationToken.None);
        await act.Should().ThrowAsync<FamilyChat.Application.Exceptions.ForbiddenException>();
    }
}
