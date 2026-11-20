using System.Security.Claims;

namespace eShop.Webhooks.FunctionalTests;

public sealed class TestAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.Request.Headers["X-Test-User"].FirstOrDefault() ?? "test-user";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", userId)],
            "Test"));
        await next(context);
    }
}
