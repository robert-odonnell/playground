using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Exceptions;
using FamilyChat.Application.Options;
using FamilyChat.Application.Utils;
using FamilyChat.Contracts.Auth;
using FamilyChat.Contracts.Common;
using FamilyChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyChat.Application.Services;

public sealed class AuthService(
    IFamilyChatDbContext dbContext,
    IEmailSender emailSender,
    ITokenService tokenService,
    ICurrentUserAccessor currentUserAccessor,
    IOptions<AuthOptions> authOptions)
{
    public async Task RequestMagicLinkAsync(MagicLinkRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email is required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || user.IsDisabled)
        {
            return;
        }

        var rawToken = tokenService.CreateOpaqueToken();
        var tokenHash = CryptoHelpers.HashToken(rawToken);
        var now = DateTime.UtcNow;

        var entity = new MagicLinkToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(authOptions.Value.MagicLinkLifetimeMinutes)
        };

        await dbContext.MagicLinkTokens.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = request.RedirectUri?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = authOptions.Value.MagicLinkBaseUrl;
        }

        var separator = baseUrl!.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var link = $"{baseUrl}{separator}email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(rawToken)}";
        await emailSender.SendMagicLinkAsync(user.Email, user.DisplayName, link, cancellationToken);
    }

    public async Task<AuthTokensDto> VerifyMagicLinkAsync(MagicLinkVerifyDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ValidationException("Email and token are required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Email.ToLower() == normalizedEmail, cancellationToken)
            ?? throw new UnauthorizedException("Invalid magic link.");

        if (user.IsDisabled)
        {
            throw new ForbiddenException("User is disabled.");
        }

        var tokenHash = CryptoHelpers.HashToken(request.Token);
        var link = await dbContext.MagicLinkTokens
            .FirstOrDefaultAsync(candidate =>
                candidate.UserId == user.Id &&
                candidate.TokenHash == tokenHash &&
                candidate.ConsumedAt == null &&
                candidate.ExpiresAt > now,
                cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired magic link.");

        link.ConsumedAt = now;

        var tokenResult = tokenService.CreateAccessToken(user);
        var rawRefreshToken = tokenService.CreateOpaqueToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = CryptoHelpers.HashToken(rawRefreshToken),
            InstallationId = string.IsNullOrWhiteSpace(request.InstallationId)
                ? "unknown"
                : request.InstallationId.Trim(),
            CreatedAt = now,
            ExpiresAt = now.AddDays(authOptions.Value.RefreshTokenLifetimeDays)
        };

        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto
        {
            AccessToken = tokenResult.Token,
            RefreshToken = rawRefreshToken,
            AccessTokenExpiresAt = tokenResult.ExpiresAt,
            User = user.ToProfileDto()
        };
    }

    public async Task<AuthTokensDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ValidationException("Refresh token is required.");
        }

        var now = DateTime.UtcNow;
        var tokenHash = CryptoHelpers.HashToken(request.RefreshToken);

        var existing = await dbContext.RefreshTokens
            .Include(refresh => refresh.User)
            .FirstOrDefaultAsync(refresh =>
                refresh.TokenHash == tokenHash &&
                refresh.RevokedAt == null &&
                refresh.ExpiresAt > now,
                cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!string.IsNullOrWhiteSpace(request.InstallationId) &&
            !string.Equals(existing.InstallationId, request.InstallationId, StringComparison.Ordinal))
        {
            throw new UnauthorizedException("Refresh token installation mismatch.");
        }

        if (existing.User is null || existing.User.IsDisabled)
        {
            throw new ForbiddenException("User is disabled.");
        }

        existing.RevokedAt = now;

        var tokenResult = tokenService.CreateAccessToken(existing.User);
        var newRawRefreshToken = tokenService.CreateOpaqueToken();

        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = CryptoHelpers.HashToken(newRawRefreshToken),
            InstallationId = existing.InstallationId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(authOptions.Value.RefreshTokenLifetimeDays)
        };

        await dbContext.RefreshTokens.AddAsync(replacement, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto
        {
            AccessToken = tokenResult.Token,
            RefreshToken = newRawRefreshToken,
            AccessTokenExpiresAt = tokenResult.ExpiresAt,
            User = existing.User.ToProfileDto()
        };
    }

    public async Task LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var hashed = CryptoHelpers.HashToken(request.RefreshToken);
        var entity = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token =>
                token.TokenHash == hashed && token.UserId == currentUserAccessor.UserId && token.RevokedAt == null,
                cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthMeDto> GetMeAsync(CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == currentUserAccessor.UserId, cancellationToken)
            ?? throw new UnauthorizedException();

        return new AuthMeDto
        {
            User = user.ToProfileDto()
        };
    }
}
