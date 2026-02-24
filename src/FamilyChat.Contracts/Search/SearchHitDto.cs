namespace FamilyChat.Contracts.Search;

public sealed class SearchHitDto
{
    public SearchHitKind Kind { get; set; }
    public Guid ConversationId { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Snippet { get; set; }
    public string? FileName { get; set; }
    public string? ShareUrl { get; set; }
}
