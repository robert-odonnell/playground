using FamilyChat.Application.Services;
using FamilyChat.Contracts.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyChat.Api.Controllers;

[ApiController]
[Authorize]
[Route("search")]
public sealed class SearchController(SearchService searchService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SearchResponseDto>> Search(
        [FromQuery] string q,
        [FromQuery] Guid? conversationId,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var response = await searchService.SearchAsync(q, conversationId, limit, cancellationToken);
        return Ok(response);
    }
}
