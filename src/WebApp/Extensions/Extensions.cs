using Inked.Basket.API.Grpc;
using Inked.WebApp;
using Inked.WebApp.Services.OrderStatus.IntegrationEvents;
using Inked.WebAppComponents.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.AI;
using OpenAI;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddAuthenticationServices();

        builder.AddRabbitMqEventBus("EventBus")
            .AddEventBusSubscriptions();

        builder.Services.AddHttpForwarderWithServiceDiscovery();

        // Application services
        builder.Services.AddScoped<BasketState>();
        builder.Services.AddSingleton<BasketService>();
        builder.Services.AddSingleton<SubmissionService>();
        builder.Services.AddSingleton<CardTypeService>();
        builder.Services.AddSingleton<CardThemeService>();
        builder.Services.AddSingleton<OrderStatusNotificationService>();
        builder.Services.AddSingleton<IProductImageUrlProvider, ProductImageUrlProvider>();
        builder.AddAIServices();

        // HTTP and GRPC client registrations
        builder.Services.AddGrpcClient<Basket.BasketClient>(o => o.Address = new Uri("http://basket-api"))
            .AddAuthToken();

        builder.Services.AddHttpClient<CatalogService>(o => o.BaseAddress = new Uri("http://catalog-api"))
            .AddApiVersion(2.0)
            .AddAuthToken();

        builder.Services.AddHttpClient<OrderingService>(o => o.BaseAddress = new Uri("http://ordering-api"))
            .AddApiVersion(1.0)
            .AddAuthToken();

        builder.Services.AddHttpClient<SubmissionService>(o => o.BaseAddress = new Uri("https://submission-api"))
            .AddApiVersion(1.0)
            .AddAuthToken();

        builder.Services.AddHttpClient<CardTypeService>(o => o.BaseAddress = new Uri("https://submission-api"))
            .AddApiVersion(1.0)
            .AddAuthToken();

        builder.Services.AddHttpClient<CardThemeService>(o => o.BaseAddress = new Uri("https://submission-api"))
            .AddApiVersion(1.0)
            .AddAuthToken();

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit =
                builder.Configuration.GetRequiredValue<long>("Kestrel:Limits:MaxRequestBodySize"); // 10MB
        });
    }

    public static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        eventBus
            .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent,
                OrderStatusChangedToAwaitingValidationIntegrationEventHandler>();
        eventBus
            .AddSubscription<OrderStatusChangedToPaidIntegrationEvent,
                OrderStatusChangedToPaidIntegrationEventHandler>();
        eventBus
            .AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent,
                OrderStatusChangedToStockConfirmedIntegrationEventHandler>();
        eventBus
            .AddSubscription<OrderStatusChangedToShippedIntegrationEvent,
                OrderStatusChangedToShippedIntegrationEventHandler>();
        eventBus
            .AddSubscription<OrderStatusChangedToCancelledIntegrationEvent,
                OrderStatusChangedToCancelledIntegrationEventHandler>();
        eventBus
            .AddSubscription<OrderStatusChangedToSubmittedIntegrationEvent,
                OrderStatusChangedToSubmittedIntegrationEventHandler>();
    }

    public static void AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        // Add Authentication services
        services.AddAuthorization();
        builder.AddDefaultAuthentication();

        // Blazor auth services
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
    }

    private static void AddAIServices(this IHostApplicationBuilder builder)
    {
        if (builder.Configuration["OllamaEnabled"] is string ollamaEnabled && bool.Parse(ollamaEnabled))
        {
            builder.AddOllamaApiClient("chat")
                .AddChatClient()
                .UseFunctionInvocation();
        }
        else
        {
            var chatModel = builder.Configuration.GetSection("AI").Get<AIOptions>()?.OpenAI?.ChatModel;
            if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("openai")) &&
                !string.IsNullOrWhiteSpace(chatModel))
            {
                builder.AddOpenAIClientFromConfiguration("openai");
                builder.Services.AddChatClient(sp =>
                        sp.GetRequiredService<OpenAIClient>().AsChatClient(chatModel ?? "gpt-4o-mini"))
                    .UseFunctionInvocation()
                    .UseOpenTelemetry(configure: t => t.EnableSensitiveData = true)
                    .UseLogging();
            }
        }
    }

    public static async Task<string?> GetBuyerIdAsync(this AuthenticationStateProvider authenticationStateProvider)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        return user.FindFirst("sub")?.Value;
    }

    public static async Task<string?> GetUserNameAsync(this AuthenticationStateProvider authenticationStateProvider)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        return user.FindFirst("name")?.Value;
    }

    internal static IEndpointConventionBuilder MapLoginAndLogout(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("authentication");

        group.MapGet("/login", OnLogin).AllowAnonymous();
        group.MapPost("/logout", OnLogout);

        return group;
    }

    private static ChallengeHttpResult OnLogin()
    {
        return TypedResults.Challenge(new AuthenticationProperties { RedirectUri = "/" });
    }

    private static SignOutHttpResult OnLogout()
    {
        return TypedResults.SignOut(new AuthenticationProperties { RedirectUri = "/" },
        [
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme
        ]);
    }
}
