using System.IO.Pipes;
using eShop.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddForwardedHeaders();

// TestStack=true swaps the Azure Container Apps environment for a Docker Compose environment used for
// reproducible, scenario-driven test runs (see testing/README.md). Everything else in the model is shared.
var isTestStack = TestStack.IsEnabled(builder.Configuration);
if (isTestStack)
{
    builder.AddTestStackEnvironment();
}
else
{
    builder.AddAzureContainerAppEnvironment("aca");
}

var redis = builder.AddRedis("redis");
var rabbitMq = builder.AddRabbitMQ("eventbus")
    .WithLifetime(ContainerLifetime.Persistent);
var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");

var launchProfileName = ShouldUseHttpForEndpoints() || isTestStack ? "http" : "https";

// Services
var identityApi = builder.AddDotnetProject("identity-api", "../Identity.API", o => o.LaunchProfileName = launchProfileName)
    .WithExternalHttpEndpoints()
    .WithReference(identityDb)
    .WithHttpHealthCheck("/health");

var identityEndpoint = identityApi.GetEndpoint(launchProfileName);
    
var basketApi = builder.AddDotnetProject("basket-api", "../Basket.API")
    .WithReference(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithEnvironment("Identity__Url", identityEndpoint);
redis.WithParentRelationship(basketApi);

var catalogApi = builder.AddDotnetProject("catalog-api", "../Catalog.API")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(catalogDb);

var orderingApi = builder.AddDotnetProject("ordering-api", "../Ordering.API")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb).WaitFor(orderDb)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("Identity__Url", identityEndpoint);

var orderProcessor = builder.AddDotnetProject("order-processor", "../OrderProcessor")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb)
    .WaitFor(orderingApi); // wait for the orderingApi to be ready because that contains the EF migrations
    
var paymentProcessor = builder.AddDotnetProject("payment-processor", "../PaymentProcessor")
    .WithReference(rabbitMq).WaitFor(rabbitMq);

var webHooksApi = builder.AddDotnetProject("webhooks-api", "../Webhooks.API")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(webhooksDb)
    .WithEnvironment("Identity__Url", identityEndpoint);

// Reverse proxies
var mobileBff = builder.AddYarp("mobile-bff")
    .WithExternalHttpEndpoints()
    .ConfigureMobileBffRoutes(catalogApi, orderingApi, identityApi);

// Apps
var webhooksClient = builder.AddDotnetProject("webhooksclient", "../WebhookClient", o => o.LaunchProfileName = launchProfileName)
    .WithReference(webHooksApi)
    .WithEnvironment("IdentityUrl", identityEndpoint);

var webApp = builder.AddDotnetProject("webapp", "../WebApp", o => o.LaunchProfileName = launchProfileName)
    .WithExternalHttpEndpoints()
    .WithUrls(c => c.Urls.ForEach(u => u.DisplayText = $"Online Store ({u.Endpoint?.EndpointName})"))
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WaitFor(identityApi)
    .WithEnvironment("IdentityUrl", identityEndpoint);

// Set UseFoundry=true to provision Microsoft Foundry for chat and embeddings.
bool useFoundry = Extensions.IsFoundryEnabled(builder.Configuration);
if (useFoundry)
{
    builder.AddFoundry(catalogApi, webApp);
}

bool useOllama = false;
if (useOllama)
{
    builder.AddOllama(catalogApi, webApp);
}

// Wire up the callback urls (self referencing)
webApp.WithEnvironment("CallBackUrl", webApp.GetEndpoint(launchProfileName));
webhooksClient.WithEnvironment("CallBackUrl", webhooksClient.GetEndpoint(launchProfileName));

// Identity has a reference to all of the apps for callback urls, this is a cyclic reference
identityApi.WithEnvironment("BasketApiClient", basketApi.GetEndpoint("http"))
           .WithEnvironment("OrderingApiClient", orderingApi.GetEndpoint("http"))
           .WithEnvironment("WebhooksApiClient", webHooksApi.GetEndpoint("http"))
           .WithEnvironment("WebhooksWebClient", webhooksClient.GetEndpoint(launchProfileName))
           .WithEnvironment("WebAppClient", webApp.GetEndpoint(launchProfileName));

if (isTestStack)
{
    // Compose-only customizations: fixed host ports, browser-reachable URLs and the seeder-off switch.
    postgres.WithTestStackService(TestStack.PostgresPort, 5432);
    redis.WithTestStackService(TestStack.RedisPort, 6379);
    rabbitMq.WithTestStackService(TestStack.RabbitMqPort, 5672);

    identityApi.WithTestStackService(TestStack.IdentityApiPort, seeded: true, env: new()
    {
        ["WebAppClient"] = TestStack.WebAppUrl,
        ["WebhooksWebClient"] = TestStack.WebhooksClientUrl,
    });
    catalogApi.WithTestStackService(TestStack.CatalogApiPort, seeded: true);
    orderingApi.WithTestStackService(TestStack.OrderingApiPort, seeded: true, env: new() { ["Identity__Url"] = TestStack.IdentityUrl });
    basketApi.WithTestStackService(TestStack.BasketApiPort, env: new() { ["Identity__Url"] = TestStack.IdentityUrl });
    webHooksApi.WithTestStackService(TestStack.WebhooksApiPort, env: new() { ["Identity__Url"] = TestStack.IdentityUrl });
    orderProcessor.WithTestStackService();
    paymentProcessor.WithTestStackService();
    mobileBff.WithTestStackService(TestStack.MobileBffPort);
    webApp.WithTestStackService(TestStack.WebAppPort, env: new()
    {
        ["IdentityUrl"] = TestStack.IdentityUrl,
        ["CallBackUrl"] = TestStack.WebAppUrl,
    });
    webhooksClient.WithTestStackService(TestStack.WebhooksClientPort, env: new()
    {
        ["IdentityUrl"] = TestStack.IdentityUrl,
        ["CallBackUrl"] = TestStack.WebhooksClientUrl,
    });
    // Webhooks.API calls the client back container-to-container, so it must not advertise the host-only localhost URL.
    webhooksClient.WithEnvironment("WebhookClientOptions__SelfUrl",
        ReferenceExpression.Create($"{webhooksClient.GetEndpoint("http")}/"));
}

builder.Build().Run();

// For test use only.
// Looks for an environment variable that forces the use of HTTP for all the endpoints. We
// are doing this for ease of running the Playwright tests in CI.
static bool ShouldUseHttpForEndpoints()
{
    const string EnvVarName = "ESHOP_USE_HTTP_ENDPOINTS";
    var envValue = Environment.GetEnvironmentVariable(EnvVarName);

    // Attempt to parse the environment variable value; return true if it's exactly "1".
    return int.TryParse(envValue, out int result) && result == 1;
}
