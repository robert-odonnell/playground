namespace FamilyChat.Contracts.Conversations;

public sealed class CreateChannelRequestDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public string? Topic { get; set; }
}
