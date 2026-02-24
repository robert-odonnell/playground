namespace FamilyChat.Contracts.Realtime;

public sealed class UnreadUpdatedDto
{
    public Guid ConversationId { get; set; }
    public int UnreadCount { get; set; }
    public int TotalUnreadConversations { get; set; }
}
