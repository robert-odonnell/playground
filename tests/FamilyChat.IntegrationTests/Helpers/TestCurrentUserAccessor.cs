using FamilyChat.Application.Abstractions;

namespace FamilyChat.IntegrationTests.Helpers;

public sealed class TestCurrentUserAccessor(Guid userId, bool isAdmin = false) : ICurrentUserAccessor
{
    public Guid UserId { get; } = userId;
    public bool IsAdmin { get; } = isAdmin;
}
