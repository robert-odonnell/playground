namespace FamilyChat.Contracts.Conversations;

public sealed class CreateDmRequestDto
{
    public Guid OtherUserId { get; set; }
}
