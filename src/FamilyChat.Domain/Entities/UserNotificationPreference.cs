namespace FamilyChat.Domain.Entities;

public sealed class UserNotificationPreference
{
    public Guid UserId { get; set; }
    public bool InAppToastsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
