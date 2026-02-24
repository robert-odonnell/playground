namespace FamilyChat.Application.Abstractions;

public interface IEmailSender
{
    Task SendMagicLinkAsync(string recipientEmail, string recipientName, string link, CancellationToken cancellationToken);
}
