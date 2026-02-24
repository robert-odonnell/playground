using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Options;
using FamilyChat.Application.Utils;
using FamilyChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyChat.AdminTool.Services;

public sealed class AdminToolService(
    IFamilyChatDbContext dbContext,
    IOptions<AuthOptions> authOptions)
{
    private const int MaxMagicLinksToReturn = 200;

    public async Task<IReadOnlyList<ManagedUser>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.IsDisabled)
            .ThenBy(user => user.DisplayName)
            .ToArrayAsync(cancellationToken);

        return users.Select(user => new ManagedUser(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsAdmin,
            user.IsDisabled,
            user.CreatedAt))
            .ToArray();
    }

    public async Task<IReadOnlyList<ActiveUserOption>> GetActiveUsersAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => !user.IsDisabled)
            .OrderBy(user => user.DisplayName)
            .ToArrayAsync(cancellationToken);

        return users.Select(user => new ActiveUserOption(user.Id, user.DisplayName, user.Email)).ToArray();
    }

    public async Task AddUserAsync(AddUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new InvalidOperationException("Email and display name are required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users
            .AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            IsAdmin = request.IsAdmin,
            IsDisabled = false,
            CreatedAt = DateTime.UtcNow
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (user.IsDisabled)
        {
            return;
        }

        if (user.IsAdmin)
        {
            var activeAdminCount = await dbContext.Users
                .CountAsync(candidate => candidate.IsAdmin && !candidate.IsDisabled, cancellationToken);

            if (activeAdminCount <= 1)
            {
                throw new InvalidOperationException("Cannot remove the only active admin user.");
            }
        }

        user.IsDisabled = true;
        var now = DateTime.UtcNow;

        var activeRefreshTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToArrayAsync(cancellationToken);

        foreach (var token in activeRefreshTokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        user.IsDisabled = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IssuedMagicLink> IssueMagicLinkAsync(IssueMagicLinkRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (user.IsDisabled)
        {
            throw new InvalidOperationException("Cannot issue a magic link for a disabled user.");
        }

        var rawToken = CryptoHelpers.CreateMagicCode(6);
        var tokenCode = CryptoHelpers.NormalizeMagicCode(rawToken);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(authOptions.Value.MagicLinkLifetimeMinutes);

        var entity = new MagicLinkToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenCode,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        await dbContext.MagicLinkTokens.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = string.IsNullOrWhiteSpace(request.RedirectUri)
            ? authOptions.Value.MagicLinkBaseUrl
            : request.RedirectUri.Trim();

        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Redirect URL is invalid. Use an absolute URL.");
        }

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var link = $"{baseUrl}{separator}email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(tokenCode)}";

        return new IssuedMagicLink(
            entity.Id,
            user.Email,
            user.DisplayName,
            tokenCode,
            link,
            entity.CreatedAt,
            entity.ExpiresAt);
    }

    public async Task<IReadOnlyList<MagicLinkHistoryItem>> GetMagicLinkHistoryAsync(
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.MagicLinkTokens
            .AsNoTracking()
            .Include(token => token.User)
            .OrderByDescending(token => token.CreatedAt)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(token => token.UserId == userId.Value);
        }

        var links = await query
            .Take(MaxMagicLinksToReturn)
            .ToArrayAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var baseUrl = authOptions.Value.MagicLinkBaseUrl;

        return links.Select(link =>
        {
            var status = link.ConsumedAt.HasValue
                ? "Consumed"
                : link.ExpiresAt <= now
                    ? "Expired"
                    : "Active";

            string? fullLink = null;
            if (!string.IsNullOrWhiteSpace(link.User?.Email) &&
                !string.IsNullOrWhiteSpace(baseUrl) &&
                Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
                fullLink =
                    $"{baseUrl}{separator}email={Uri.EscapeDataString(link.User.Email)}&token={Uri.EscapeDataString(link.TokenHash)}";
            }

            return new MagicLinkHistoryItem(
                link.Id,
                link.UserId,
                link.User?.DisplayName ?? "(unknown)",
                link.User?.Email ?? "(unknown)",
                link.TokenHash,
                link.CreatedAt,
                link.ExpiresAt,
                link.ConsumedAt,
                status,
                fullLink);
        }).ToArray();
    }
}

public sealed record ManagedUser(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsAdmin,
    bool IsDisabled,
    DateTime CreatedAt);

public sealed record ActiveUserOption(Guid Id, string DisplayName, string Email);

public sealed record AddUserRequest(string Email, string DisplayName, bool IsAdmin);

public sealed record IssueMagicLinkRequest(Guid UserId, string? RedirectUri);

public sealed record IssuedMagicLink(
    Guid Id,
    string Email,
    string DisplayName,
    string TokenCode,
    string Link,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public sealed record MagicLinkHistoryItem(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string Email,
    string TokenCode,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ConsumedAt,
    string Status,
    string? Link);
