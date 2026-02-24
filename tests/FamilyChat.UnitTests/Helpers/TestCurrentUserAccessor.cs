using FamilyChat.Application.Abstractions;

namespace FamilyChat.UnitTests.Helpers;

public sealed class TestCurrentUserAccessor(Guid userId, bool isAdmin = false) : ICurrentUserAccessor
{
    public Guid UserId { get; } = userId;
    public bool IsAdmin { get; } = isAdmin;
}
