using FamilyChat.Application.Abstractions;
using FamilyChat.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FamilyChat.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendMagicLinkAsync(
        string recipientEmail,
        string recipientName,
        string link,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.Host))
        {
            logger.LogInformation("SMTP host not configured. Magic link for {Email}: {Link}", recipientEmail, link);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.Value.FromName, options.Value.FromEmail));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = "Your Family Chat sign-in link";

        message.Body = new TextPart("plain")
        {
            Text = $"Use this link to sign in:\n\n{link}\n\nThis link expires soon."
        };

        using var client = new SmtpClient();

        var secureSocket = options.Value.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(options.Value.Host, options.Value.Port, secureSocket, cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.Value.Username))
        {
            await client.AuthenticateAsync(options.Value.Username, options.Value.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
