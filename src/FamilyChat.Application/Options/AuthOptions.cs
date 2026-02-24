namespace FamilyChat.Application.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public string MagicLinkBaseUrl { get; set; } = "http://localhost:5000/auth/verify";
    public int MagicLinkLifetimeMinutes { get; set; } = 15;
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
