using FamilyChat.Application.Options;
using FamilyChat.Application.Services;
using FamilyChat.Contracts.Auth;
using FamilyChat.Domain.Entities;
using FamilyChat.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace FamilyChat.IntegrationTests;

public sealed class AuthFlowIntegrationTests
{
    [Fact]
    public async Task RequestVerifyRefreshLogout_ShouldWorkEndToEnd()
    {
        var userId = Guid.NewGuid();
        var db = TestDbContextFactory.Create(nameof(RequestVerifyRefreshLogout_ShouldWorkEndToEnd));
        db.Users.Add(new User
        {
            Id = userId,
            Email = "member@test.local",
            DisplayName = "Member",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var fakeEmail = new FakeEmailSender();
        var fakeTokenService = new FakeTokenService();
        var currentUser = new TestCurrentUserAccessor(userId);
        var options = Options.Create(new AuthOptions
        {
            MagicLinkBaseUrl = "https://example.test/sign-in",
            MagicLinkLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 30
        });

        var service = new AuthService(db, fakeEmail, fakeTokenService, currentUser, options);

        await service.RequestMagicLinkAsync(new MagicLinkRequestDto
        {
            Email = "member@test.local",
            InstallationId = "install-1",
            Platform = "Windows"
        }, CancellationToken.None);

        fakeEmail.LastLink.Should().NotBeNull();

        var uri = new Uri(fakeEmail.LastLink!);
        var queryMap = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split('=', 2))
            .ToDictionary(item => Uri.UnescapeDataString(item[0]), item => Uri.UnescapeDataString(item[1]));

        var verify = await service.VerifyMagicLinkAsync(new MagicLinkVerifyDto
        {
            Email = queryMap["email"],
            Token = queryMap["token"],
            InstallationId = "install-1",
            Platform = "Windows"
        }, CancellationToken.None);

        verify.AccessToken.Should().NotBeNullOrWhiteSpace();
        verify.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var refreshed = await service.RefreshAsync(new RefreshTokenRequestDto
        {
            RefreshToken = verify.RefreshToken,
            InstallationId = "install-1"
        }, CancellationToken.None);

        refreshed.AccessToken.Should().NotBe(verify.AccessToken);

        await service.LogoutAsync(new LogoutRequestDto
        {
            RefreshToken = refreshed.RefreshToken
        }, CancellationToken.None);

        db.RefreshTokens.Count(token => token.RevokedAt != null).Should().BeGreaterThan(0);
    }
}
