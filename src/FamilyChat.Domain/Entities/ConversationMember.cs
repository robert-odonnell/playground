namespace FamilyChat.Domain.Entities;

public sealed class ConversationMember
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }

    public Conversation? Conversation { get; set; }
    public User? User { get; set; }
}
