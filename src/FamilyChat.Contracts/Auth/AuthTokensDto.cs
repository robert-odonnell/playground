using FamilyChat.Contracts.Common;

namespace FamilyChat.Contracts.Auth;

public sealed class AuthTokensDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = new(Guid.Empty, string.Empty, string.Empty, false, false);
}
