using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Inked.WebhookClient.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddAuthenticationServices();

        // Application services
        builder.Services.AddOptions<WebhookClientOptions>().BindConfiguration(nameof(WebhookClientOptions));
        builder.Services.AddSingleton<HooksRepository>();

        // HTTP client registrations
        builder.Services.AddHttpClient<WebhooksClient>(o => o.BaseAddress = new Uri("http://webhooks-api"))
            .AddApiVersion(1.0)
            .AddAuthToken();
    }

    public static void AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;
        // Add Authentication services
        services.AddAuthorization();
        builder.AddDefaultAuthentication();

        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
    }
}
