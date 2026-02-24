using FamilyChat.Contracts.Enums;

namespace FamilyChat.Contracts.Messages;

public sealed class AttachmentInputDto
{
    public AttachmentProvider Provider { get; set; }
    public string? FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
}
