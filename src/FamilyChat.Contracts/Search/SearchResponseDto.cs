namespace FamilyChat.Contracts.Search;

public sealed class SearchResponseDto
{
    public IReadOnlyList<SearchHitDto> Hits { get; set; } = [];
}
