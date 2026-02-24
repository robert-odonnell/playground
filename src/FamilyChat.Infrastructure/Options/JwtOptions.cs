namespace FamilyChat.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "FamilyChat";
    public string Audience { get; set; } = "FamilyChat.Client";
    public string SigningKey { get; set; } = "replace-with-long-random-key";
}
