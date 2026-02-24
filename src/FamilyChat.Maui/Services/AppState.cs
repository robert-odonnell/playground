using FamilyChat.Contracts.Common;

namespace FamilyChat.Maui.Services;

public sealed class AppState
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserProfileDto? CurrentUser { get; set; }
    public Guid? ActiveConversationId { get; set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && CurrentUser is not null;

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        CurrentUser = null;
        ActiveConversationId = null;
    }
}
