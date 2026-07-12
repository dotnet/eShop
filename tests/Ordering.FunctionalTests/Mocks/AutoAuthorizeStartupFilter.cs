using Microsoft.AspNetCore.Hosting;

namespace eShop.Ordering.FunctionalTests.Mocks;

internal sealed class AutoAuthorizeStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<AutoAuthorizeMiddleware>();
            next(builder);
        };
    }
}
