using FamilyChat.Application.Abstractions;
using NUlid;

namespace FamilyChat.Infrastructure.Ids;

public sealed class UlidGenerator : IUlidGenerator
{
    public string NewUlid()
    {
        return Ulid.NewUlid().ToString();
    }
}
