using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FamilyChat.AdminTool.Models.MagicLinks;

public sealed class MagicLinksIndexViewModel
{
    public IssueMagicLinkInputModel IssueForm { get; set; } = new();
    public IReadOnlyList<SelectListItem> ActiveUsers { get; set; } = [];
    public IReadOnlyList<MagicLinkRowViewModel> History { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? GeneratedLink { get; set; }
    public DateTime? GeneratedLinkExpiresAt { get; set; }
}

public sealed class IssueMagicLinkInputModel
{
    [Required]
    public Guid UserId { get; set; }

    [Display(Name = "Redirect URL")]
    [Url]
    public string? RedirectUri { get; set; }
}

public sealed class MagicLinkRowViewModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TokenCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Link { get; set; }
}
