namespace FamilyChat.Domain.Entities;

public sealed class DirectMessagePair
{
    public Guid UserAId { get; set; }
    public Guid UserBId { get; set; }
    public Guid ConversationId { get; set; }

    public Conversation? Conversation { get; set; }
}
