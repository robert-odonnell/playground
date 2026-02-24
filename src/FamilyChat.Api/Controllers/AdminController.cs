using FamilyChat.Application.Services;
using FamilyChat.Contracts.Admin;
using FamilyChat.Contracts.Common;
using FamilyChat.Contracts.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyChat.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("admin")]
public sealed class AdminController(AdminService adminService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var response = await adminService.GetUsersAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("users")]
    public async Task<ActionResult<AdminUserDto>> CreateUser(
        [FromBody] AdminCreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await adminService.CreateUserAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("users/{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(
        Guid id,
        [FromBody] AdminUpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await adminService.UpdateUserAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("channels")]
    public async Task<ActionResult<IReadOnlyList<AdminChannelDto>>> GetChannels(CancellationToken cancellationToken)
    {
        var response = await adminService.GetChannelsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("channels")]
    public async Task<ActionResult<AdminChannelDto>> CreateChannel(
        [FromBody] CreateChannelRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await adminService.CreateChannelAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("channels/{id:guid}")]
    public async Task<ActionResult<AdminChannelDto>> UpdateChannel(
        Guid id,
        [FromBody] UpdateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await adminService.UpdateChannelAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("channels/{id:guid}/members")]
    public async Task<ActionResult<ApiAckDto>> AddMember(
        Guid id,
        [FromBody] AddMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        await adminService.AddChannelMemberAsync(id, request.UserId, cancellationToken);
        return Ok(new ApiAckDto());
    }

    [HttpDelete("channels/{id:guid}/members/{uid:guid}")]
    public async Task<ActionResult<ApiAckDto>> RemoveMember(
        Guid id,
        Guid uid,
        CancellationToken cancellationToken)
    {
        await adminService.RemoveChannelMemberAsync(id, uid, cancellationToken);
        return Ok(new ApiAckDto());
    }
}
