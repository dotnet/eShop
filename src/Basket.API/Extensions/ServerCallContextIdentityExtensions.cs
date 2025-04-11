#nullable enable

namespace Inked.Basket.API.Extensions;

internal static class ServerCallContextIdentityExtensions
{
    public static string? GetUserIdentity(this ServerCallContext context)
    {
        return context.GetHttpContext().User.FindFirst("sub")?.Value;
    }

    public static string? GetUserName(this ServerCallContext context)
    {
        return context.GetHttpContext().User.FindFirst(x => x.Type == ClaimTypes.Name)?.Value;
    }
}
