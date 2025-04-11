using System.Security.Claims;

namespace inked.ServiceDefaults;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value;
    }

    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(x => x.Type == ClaimTypes.Name)?.Value;
    }
}
