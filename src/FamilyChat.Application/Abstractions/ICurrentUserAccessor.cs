namespace FamilyChat.Application.Abstractions;

public interface ICurrentUserAccessor
{
    Guid UserId { get; }
    bool IsAdmin { get; }
}
