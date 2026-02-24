namespace FamilyChat.Contracts.Common;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsAdmin,
    bool IsDisabled);
