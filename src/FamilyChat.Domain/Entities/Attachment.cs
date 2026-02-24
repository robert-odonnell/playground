using FamilyChat.Domain.Enums;

namespace FamilyChat.Domain.Entities;

public sealed class Attachment
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public AttachmentProvider Provider { get; set; }
    public string? FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Message? Message { get; set; }
}
