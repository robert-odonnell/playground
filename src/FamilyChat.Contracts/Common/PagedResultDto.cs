namespace FamilyChat.Contracts.Common;

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}
