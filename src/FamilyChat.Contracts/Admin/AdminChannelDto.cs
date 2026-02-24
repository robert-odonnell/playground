namespace FamilyChat.Contracts.Admin;

public sealed class AdminChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public bool IsPrivate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
