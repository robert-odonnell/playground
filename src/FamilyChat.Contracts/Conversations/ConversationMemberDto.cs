namespace FamilyChat.Contracts.Conversations;

public sealed class ConversationMemberDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
