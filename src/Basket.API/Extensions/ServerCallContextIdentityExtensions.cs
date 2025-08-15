#nullable enable

namespace eShop.Basket.API.Extensions;

internal static class ServerCallContextIdentityExtensions
{
    public static string? GetUserIdentity(this ServerCallContext context) 
    {
        // Check if authentication is disabled
        var config = context.GetHttpContext().RequestServices.GetService<IConfiguration>();
        var disableAuth = config?.GetValue<bool>("DisableAuth") ?? false;
        
        if (disableAuth)
        {
            // Return a default user ID when authentication is disabled
            return "demo-user-123";
        }
        
        return context.GetHttpContext().User.FindFirst("sub")?.Value;
    }
    
    public static string? GetUserName(this ServerCallContext context) 
    {
        // Check if authentication is disabled
        var config = context.GetHttpContext().RequestServices.GetService<IConfiguration>();
        var disableAuth = config?.GetValue<bool>("DisableAuth") ?? false;
        
        if (disableAuth)
        {
            // Return a default user name when authentication is disabled
            return "Demo User";
        }
        
        return context.GetHttpContext().User.FindFirst(x => x.Type == ClaimTypes.Name)?.Value;
    }
}
