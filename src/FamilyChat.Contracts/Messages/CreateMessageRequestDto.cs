namespace FamilyChat.Contracts.Messages;

public sealed class CreateMessageRequestDto
{
    public string Body { get; set; } = string.Empty;
    public IReadOnlyList<Guid>? Mentions { get; set; }
    public IReadOnlyList<AttachmentInputDto>? Attachments { get; set; }
}
