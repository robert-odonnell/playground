namespace FamilyChat.Contracts.Admin;

public sealed class AdminUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
