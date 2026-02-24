namespace FamilyChat.Contracts.Admin;

public sealed class AdminCreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
