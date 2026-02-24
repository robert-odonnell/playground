namespace FamilyChat.Domain.Entities;

public sealed class Message
{
    public string Id { get; set; } = string.Empty;
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string MentionUserIdsJson { get; set; } = "[]";
    public string ReactionsJson { get; set; } = "{}";
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Conversation? Conversation { get; set; }
    public User? Sender { get; set; }
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
