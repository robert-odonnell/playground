using FamilyChat.AdminTool.Models.MagicLinks;
using FamilyChat.AdminTool.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FamilyChat.AdminTool.Controllers;

public sealed class MagicLinksController(AdminToolService adminToolService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid? userId, CancellationToken cancellationToken)
    {
        var model = await BuildModelAsync(new IssueMagicLinkInputModel { UserId = userId ?? Guid.Empty }, userId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(
        [Bind(Prefix = "IssueForm")] IssueMagicLinkInputModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildModelAsync(input, input.UserId == Guid.Empty ? null : input.UserId, cancellationToken);
            return View(nameof(Index), invalidModel);
        }

        try
        {
            var issued = await adminToolService.IssueMagicLinkAsync(
                new IssueMagicLinkRequest(input.UserId, input.RedirectUri),
                cancellationToken);

            TempData["Success"] = "Magic link issued.";
            TempData["GeneratedLink"] = issued.Link;
            TempData["GeneratedLinkExpiresAt"] = issued.ExpiresAt.ToString("O");

            return RedirectToAction(nameof(Index), new { userId = input.UserId });
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            var erroredModel = await BuildModelAsync(input, input.UserId == Guid.Empty ? null : input.UserId, cancellationToken);
            return View(nameof(Index), erroredModel);
        }
    }

    private async Task<MagicLinksIndexViewModel> BuildModelAsync(
        IssueMagicLinkInputModel issueInput,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var activeUsers = await adminToolService.GetActiveUsersAsync(cancellationToken);
        if (issueInput.UserId == Guid.Empty && activeUsers.Count > 0)
        {
            issueInput.UserId = activeUsers[0].Id;
        }

        var selectedUserId = userId ?? (issueInput.UserId == Guid.Empty ? null : issueInput.UserId);
        var history = await adminToolService.GetMagicLinkHistoryAsync(selectedUserId, cancellationToken);

        DateTime? generatedLinkExpiresAt = null;
        if (TempData["GeneratedLinkExpiresAt"] is string rawExpiresAt &&
            DateTime.TryParse(rawExpiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedExpiresAt))
        {
            generatedLinkExpiresAt = parsedExpiresAt;
        }

        return new MagicLinksIndexViewModel
        {
            IssueForm = issueInput,
            ActiveUsers = activeUsers
                .Select(user => new SelectListItem
                {
                    Value = user.Id.ToString(),
                    Text = $"{user.DisplayName} ({user.Email})",
                    Selected = issueInput.UserId == user.Id
                })
                .ToArray(),
            History = history
                .Select(link => new MagicLinkRowViewModel
                {
                    Id = link.Id,
                    UserId = link.UserId,
                    DisplayName = link.DisplayName,
                    Email = link.Email,
                    TokenCode = link.TokenCode,
                    CreatedAt = link.CreatedAt,
                    ExpiresAt = link.ExpiresAt,
                    ConsumedAt = link.ConsumedAt,
                    Status = link.Status,
                    Link = link.Link
                })
                .ToArray(),
            SuccessMessage = TempData["Success"] as string,
            ErrorMessage = TempData["Error"] as string,
            GeneratedLink = TempData["GeneratedLink"] as string,
            GeneratedLinkExpiresAt = generatedLinkExpiresAt
        };
    }
}
