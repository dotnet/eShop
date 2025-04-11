namespace Inked.Ordering.API.Infrastructure.Services;

public class IdentityService(IHttpContextAccessor context) : IIdentityService
{
    public string GetUserIdentity()
    {
        return context.HttpContext?.User.FindFirst("sub")?.Value;
    }

    public string GetUserName()
    {
        return context.HttpContext?.User.Identity?.Name;
    }
}
