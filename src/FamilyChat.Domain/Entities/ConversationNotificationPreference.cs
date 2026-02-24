namespace FamilyChat.Domain.Entities;

public sealed class ConversationNotificationPreference
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public bool IsMuted { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Conversation? Conversation { get; set; }
    public User? User { get; set; }
}
