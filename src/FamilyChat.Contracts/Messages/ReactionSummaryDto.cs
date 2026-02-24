namespace FamilyChat.Contracts.Messages;

public sealed class ReactionSummaryDto
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool ReactedByMe { get; set; }
}
