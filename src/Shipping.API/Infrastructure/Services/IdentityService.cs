using System.Security.Claims;

namespace eShop.Shipping.API.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly IHttpContextAccessor _context;

    public IdentityService(IHttpContextAccessor context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public string GetUserIdentity()
    {
        return _context.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
    }

    public string GetUserName()
    {
        return _context.HttpContext?.User.FindFirst(x => x.Type == ClaimTypes.Name)?.Value ?? string.Empty;
    }
}
