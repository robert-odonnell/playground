namespace FamilyChat.Contracts.Auth;

public sealed class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
    public string InstallationId { get; set; } = string.Empty;
}
