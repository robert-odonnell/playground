using FamilyChat.Contracts.Enums;

namespace FamilyChat.Contracts.Messages;

public sealed class AttachmentDto
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public AttachmentProvider Provider { get; set; }
    public string? FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
