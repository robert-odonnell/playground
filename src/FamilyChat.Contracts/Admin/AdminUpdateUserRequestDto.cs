namespace FamilyChat.Contracts.Admin;

public sealed class AdminUpdateUserRequestDto
{
    public string? DisplayName { get; set; }
    public bool? IsAdmin { get; set; }
    public bool? IsDisabled { get; set; }
}
