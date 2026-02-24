namespace FamilyChat.Contracts.Messages;

public sealed class UpdateMessageRequestDto
{
    public string Body { get; set; } = string.Empty;
    public IReadOnlyList<Guid>? Mentions { get; set; }
}
