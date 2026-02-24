using FamilyChat.Application.Services;
using FamilyChat.Contracts.Auth;
using FamilyChat.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyChat.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("magic-link/request")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiAckDto>> RequestMagicLink(
        [FromBody] MagicLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        await authService.RequestMagicLinkAsync(request, cancellationToken);
        return Ok(new ApiAckDto());
    }

    [HttpPost("magic-link/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensDto>> VerifyMagicLink(
        [FromBody] MagicLinkVerifyDto request,
        CancellationToken cancellationToken)
    {
        var response = await authService.VerifyMagicLinkAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensDto>> Refresh(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiAckDto>> Logout(
        [FromBody] LogoutRequestDto request,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return Ok(new ApiAckDto());
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthMeDto>> Me(CancellationToken cancellationToken)
    {
        var response = await authService.GetMeAsync(cancellationToken);
        return Ok(response);
    }
}
