using FamilyChat.Api.Realtime;
using FamilyChat.Contracts.Messages;
using FamilyChat.Contracts.Realtime;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace FamilyChat.RealtimeTests;

public sealed class SignalRRealtimePublisherTests
{
    [Fact]
    public async Task PublishMessageCreated_ShouldSendToConversationGroup()
    {
        var conversationId = Guid.NewGuid();
        var groupName = $"conversation:{conversationId:D}";

        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(item => item.SendCoreAsync(RealtimeEvents.MessageCreated, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(item => item.Group(groupName)).Returns(proxy.Object);

        var hubContext = new Mock<IHubContext<RealtimeHub>>();
        hubContext.SetupGet(item => item.Clients).Returns(clients.Object);

        var publisher = new SignalRRealtimePublisher(hubContext.Object);

        await publisher.PublishMessageCreatedAsync(conversationId, new MessageDto
        {
            Id = "01HFYK4PZQ7QEVJDH85G1D1RZW",
            ConversationId = conversationId,
            SenderId = Guid.NewGuid(),
            Body = "hello",
            CreatedAt = DateTime.UtcNow
        }, CancellationToken.None);

        proxy.Verify(
            item => item.SendCoreAsync(RealtimeEvents.MessageCreated, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishUnreadUpdated_ShouldSendToUserChannel()
    {
        var userId = Guid.NewGuid();

        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(item => item.SendCoreAsync(RealtimeEvents.UnreadUpdated, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(item => item.User(userId.ToString())).Returns(proxy.Object);

        var hubContext = new Mock<IHubContext<RealtimeHub>>();
        hubContext.SetupGet(item => item.Clients).Returns(clients.Object);

        var publisher = new SignalRRealtimePublisher(hubContext.Object);

        await publisher.PublishUnreadUpdatedAsync(userId, new UnreadUpdatedDto
        {
            ConversationId = Guid.NewGuid(),
            UnreadCount = 3,
            TotalUnreadConversations = 2
        }, CancellationToken.None);

        proxy.Verify(
            item => item.SendCoreAsync(RealtimeEvents.UnreadUpdated, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
