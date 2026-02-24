using FamilyChat.Domain.Enums;

namespace FamilyChat.Domain.Entities;

public sealed class Conversation
{
    public Guid Id { get; set; }
    public ConversationType Type { get; set; }
    public string? Name { get; set; }
    public string? Topic { get; set; }
    public bool IsPrivate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
