using FamilyChat.Application.Abstractions;

namespace FamilyChat.IntegrationTests.Helpers;

public sealed class FakeEmailSender : IEmailSender
{
    public string? LastLink { get; private set; }

    public Task SendMagicLinkAsync(string recipientEmail, string recipientName, string link, CancellationToken cancellationToken)
    {
        LastLink = link;
        return Task.CompletedTask;
    }
}
