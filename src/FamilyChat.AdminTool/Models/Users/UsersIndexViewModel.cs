using System.ComponentModel.DataAnnotations;

namespace FamilyChat.AdminTool.Models.Users;

public sealed class UsersIndexViewModel
{
    public AddUserInputModel AddUser { get; set; } = new();
    public IReadOnlyList<UserRowViewModel> ActiveUsers { get; set; } = [];
    public IReadOnlyList<UserRowViewModel> RemovedUsers { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class AddUserInputModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
}

public sealed class UserRowViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
