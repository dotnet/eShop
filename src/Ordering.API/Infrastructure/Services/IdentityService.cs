namespace eShop.Ordering.API.Infrastructure.Services;

public class IdentityService(IHttpContextAccessor context, IConfiguration configuration) : IIdentityService
{
    public string GetUserIdentity()
    {
        // Check if authentication is disabled
        var disableAuth = configuration.GetValue<bool>("DisableAuth");
        
        if (disableAuth)
        {
            // Return a default user ID when authentication is disabled
            return "demo-user-123";
        }
        
        return context.HttpContext?.User.FindFirst("sub")?.Value;
    }

    public string GetUserName()
    {
        // Check if authentication is disabled
        var disableAuth = configuration.GetValue<bool>("DisableAuth");
        
        if (disableAuth)
        {
            // Return a default user name when authentication is disabled
            return "Demo User";
        }
        
        return context.HttpContext?.User.Identity?.Name;
    }
}
