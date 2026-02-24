using FamilyChat.Application.Abstractions;
using FamilyChat.Domain.Entities;

namespace FamilyChat.IntegrationTests.Helpers;

public sealed class FakeTokenService : ITokenService
{
    private int _counter;

    public AccessTokenResult CreateAccessToken(User user)
    {
        _counter++;
        return new AccessTokenResult($"access-{_counter}", DateTime.UtcNow.AddHours(1));
    }

    public string CreateOpaqueToken()
    {
        _counter++;
        return $"opaque-token-{_counter}";
    }
}
