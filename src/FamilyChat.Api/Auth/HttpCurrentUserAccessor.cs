using System.IdentityModel.Tokens.Jwt;
using FamilyChat.Application.Abstractions;
using FamilyChat.Application.Exceptions;

namespace FamilyChat.Api.Auth;

public sealed class HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public Guid UserId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (raw is null || !Guid.TryParse(raw, out var userId))
            {
                throw new UnauthorizedException();
            }

            return userId;
        }
    }

    public bool IsAdmin
    {
        get
        {
            return string.Equals(
                httpContextAccessor.HttpContext?.User.FindFirst("is_admin")?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
