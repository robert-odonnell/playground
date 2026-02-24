namespace FamilyChat.Contracts.Auth;

public sealed class MagicLinkRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string InstallationId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
}
