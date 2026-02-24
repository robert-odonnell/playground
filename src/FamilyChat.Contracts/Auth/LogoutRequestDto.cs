namespace FamilyChat.Contracts.Auth;

public sealed class LogoutRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
