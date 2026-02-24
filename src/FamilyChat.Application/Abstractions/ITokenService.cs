using FamilyChat.Domain.Entities;

namespace FamilyChat.Application.Abstractions;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user);
    string CreateOpaqueToken();
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);
