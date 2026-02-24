namespace FamilyChat.Contracts.Conversations;

public sealed class UpdateConversationRequestDto
{
    public string? Name { get; set; }
    public string? Topic { get; set; }
    public bool? IsPrivate { get; set; }
}
