using FamilyChat.AdminTool.Models.Users;
using FamilyChat.AdminTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyChat.AdminTool.Controllers;

public sealed class UsersController(AdminToolService adminToolService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await BuildModelAsync(new AddUserInputModel(), cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        [Bind(Prefix = "AddUser")] AddUserInputModel input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildModelAsync(input, cancellationToken);
            return View(nameof(Index), invalidModel);
        }

        try
        {
            await adminToolService.AddUserAsync(
                new AddUserRequest(input.Email, input.DisplayName, input.IsAdmin),
                cancellationToken);

            TempData["Success"] = "User added.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            var erroredModel = await BuildModelAsync(input, cancellationToken);
            return View(nameof(Index), erroredModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await adminToolService.RemoveUserAsync(id, cancellationToken);
            TempData["Success"] = "User removed (disabled).";
        }
        catch (Exception exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await adminToolService.RestoreUserAsync(id, cancellationToken);
            TempData["Success"] = "User restored.";
        }
        catch (Exception exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<UsersIndexViewModel> BuildModelAsync(
        AddUserInputModel addUserInput,
        CancellationToken cancellationToken)
    {
        var users = await adminToolService.GetUsersAsync(cancellationToken);
        var activeUsers = users
            .Where(user => !user.IsDisabled)
            .Select(user => new UserRowViewModel
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsAdmin = user.IsAdmin,
                IsDisabled = user.IsDisabled,
                CreatedAt = user.CreatedAt
            })
            .ToArray();

        var removedUsers = users
            .Where(user => user.IsDisabled)
            .Select(user => new UserRowViewModel
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsAdmin = user.IsAdmin,
                IsDisabled = user.IsDisabled,
                CreatedAt = user.CreatedAt
            })
            .ToArray();

        return new UsersIndexViewModel
        {
            AddUser = addUserInput,
            ActiveUsers = activeUsers,
            RemovedUsers = removedUsers,
            SuccessMessage = TempData["Success"] as string,
            ErrorMessage = TempData["Error"] as string
        };
    }
}
