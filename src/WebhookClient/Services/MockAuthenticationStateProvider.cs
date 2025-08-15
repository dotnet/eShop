using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace eShop.WebhookClient.Services;

public class MockAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _user;

    public MockAuthenticationStateProvider()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Demo User"),
            new Claim(ClaimTypes.NameIdentifier, "demo-user-123"),
            new Claim("sub", "demo-user-123"),
            new Claim("name", "Demo User"),
            new Claim("preferred_username", "demo")
        };

        var identity = new ClaimsIdentity(claims, "Mock");
        _user = new ClaimsPrincipal(identity);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_user));
    }
}