namespace FamilyChat.Domain.Entities;

public sealed class ReadState
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime LastReadAt { get; set; }

    public Conversation? Conversation { get; set; }
    public User? User { get; set; }
}
