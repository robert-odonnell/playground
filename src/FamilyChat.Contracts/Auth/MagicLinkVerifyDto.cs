namespace FamilyChat.Contracts.Auth;

public sealed class MagicLinkVerifyDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string InstallationId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}
