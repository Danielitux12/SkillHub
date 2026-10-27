using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.API.Common;

public static class CurrentUserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? user.FindFirst("sub")?.Value;

        if (Guid.TryParse(idClaim, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }

    public static string GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
               ?? user.FindFirst(JwtRegisteredClaimNames.Email)?.Value
               ?? user.FindFirst("email")?.Value
               ?? string.Empty;
    }

    public static bool IsEmployee(this ClaimsPrincipal user)
    {
        return user.IsInRole(Roles.Employee) ||
               user.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
                                    c.Value.Equals(Roles.Employee, StringComparison.OrdinalIgnoreCase));
    }
}
