namespace FamilyChat.Contracts.Messages;

public sealed class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public IReadOnlyList<Guid> MentionUserIds { get; set; } = [];
    public Dictionary<string, Guid[]> Reactions { get; set; } = [];
    public IReadOnlyList<ReactionSummaryDto> ReactionSummaries { get; set; } = [];
    public IReadOnlyList<AttachmentDto> Attachments { get; set; } = [];
}
