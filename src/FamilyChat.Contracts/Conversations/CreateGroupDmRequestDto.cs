namespace FamilyChat.Contracts.Conversations;

public sealed class CreateGroupDmRequestDto
{
    public IReadOnlyList<Guid> UserIds { get; set; } = [];
    public string? Name { get; set; }
}
