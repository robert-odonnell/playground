using FamilyChat.Contracts.Common;

namespace FamilyChat.Contracts.Auth;

public sealed class AuthMeDto
{
    public UserProfileDto User { get; set; } = new(Guid.Empty, string.Empty, string.Empty, false, false);
}
