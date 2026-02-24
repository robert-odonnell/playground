using FamilyChat.Contracts.Enums;

namespace FamilyChat.Contracts.Conversations;

public sealed class ConversationDto
{
    public Guid Id { get; set; }
    public ConversationType Type { get; set; }
    public string? Name { get; set; }
    public string? Topic { get; set; }
    public bool IsPrivate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public IReadOnlyList<ConversationMemberDto> Members { get; set; } = [];
}
