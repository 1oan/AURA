using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aura.Application.Common.Interfaces;

namespace Aura.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User
            .FindFirst(JwtRegisteredClaimNames.Sub)
            ?? _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier);

        if (claim is null || !Guid.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated.");

        return userId;
    }
}
