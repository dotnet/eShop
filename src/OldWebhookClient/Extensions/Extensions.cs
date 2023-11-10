namespace WebhookClient;

internal static class Extensions
{
    public static IHostApplicationBuilder AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        var identityUrl = configuration["IdentityUrl"];
        var callBackUrl = configuration["CallBackUrl"];

        // Add Authentication services
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(setup => setup.ExpireTimeSpan = TimeSpan.FromHours(2))
        .AddOpenIdConnect(options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = identityUrl.ToString();
            options.SignedOutRedirectUri = callBackUrl.ToString();
            options.ClientId = "webhooksclient";
            options.ClientSecret = "secret";
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.RequireHttpsMetadata = false;
            options.Scope.Add("openid");
            options.Scope.Add("webhooks");
        });

        return builder;
    }

    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IWebhooksClient, WebhooksClient>(o => o.BaseAddress = new("http://webhooks-api")).AddAuthToken();
        builder.Services.AddSingleton<IHooksRepository, InMemoryHooksRepository>();

        builder.Services.AddOptions<WebhookClientOptions>()
            .BindConfiguration(nameof(WebhookClientOptions));
    }
}
