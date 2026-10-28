using System.Security.Claims;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Web.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value;

        return Guid.TryParse(claim, out var guid) ? guid : Guid.Empty;
    }

    public static string GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? user.Identity?.Name
            ?? string.Empty;
    }

    public static bool IsEmployee(this ClaimsPrincipal user) => user.IsInRole(Roles.Employee);
    public static bool IsParticipant(this ClaimsPrincipal user) => user.IsInRole(Roles.Participant);
}
