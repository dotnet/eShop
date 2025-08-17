using eShop.AppHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddForwardedHeaders();

// Configure OTLP endpoint for secure telemetry collection
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://aspire-dashboard:18889";
var otlpApiKey = "bd556687-b71b-4065-95bb-f01077fed6cb1c4ef63a-bc26-4a4c-b29b-f6f4479a4ec6";
var otlpHeaders = $"x-otlp-api-key={otlpApiKey}";

// Configure dashboard with API key authentication
builder.Configuration["DOTNET_DASHBOARD_OTLP_AUTH_MODE"] = "ApiKey";
builder.Configuration["DOTNET_DASHBOARD_OTLP_PRIMARY_API_KEY"] = otlpApiKey;
builder.Configuration["DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS"] = "false";

builder.Services.Configure<Microsoft.Extensions.Logging.LoggerFilterOptions>(options =>
{
    options.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
});

var redis = builder.AddRedis("redis");
var rabbitMq = builder.AddRabbitMQ("eventbus")
    .WithLifetime(ContainerLifetime.Persistent);
var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "md5")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=md5 --auth-local=md5")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");
// var identityDb = postgres.AddDatabase("identitydb"); // Identity disabled
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");

var launchProfileName = ShouldUseHttpForEndpoints() ? "http" : "https";

// Services
// Identity API disabled for this deployment
// var identityApi = builder.AddProject<Projects.Identity_API>("identity-api", launchProfileName)
//     .WithExternalHttpEndpoints()
//     .WithEnvironment("Kestrel__Endpoints__Http__Url", "http://0.0.0.0:8080")
//     .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
//     .WithEnvironment("IssuerUri", "http://localhost:31671")
//     .WithEnvironment("DisableAuth", "true")
//     .WithReference(identityDb);

// var identityEndpoint = identityApi.GetEndpoint(launchProfileName);

var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    // .WithEnvironment("Identity__Url", identityEndpoint) // Identity disabled
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);
redis.WithParentRelationship(basketApi);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(catalogDb)
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);

var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb).WaitFor(orderDb)
    .WithHttpHealthCheck("/health")
    // .WithEnvironment("Identity__Url", identityEndpoint) // Identity disabled
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);

builder.AddProject<Projects.OrderProcessor>("order-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb)
    .WaitFor(orderingApi) // wait for the orderingApi to be ready because that contains the EF migrations
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);

builder.AddProject<Projects.PaymentProcessor>("payment-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);

var webHooksApi = builder.AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(webhooksDb)
    // .WithEnvironment("Identity__Url", identityEndpoint) // Identity disabled
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);

// Reverse proxies
builder.AddProject<Projects.Mobile_Bff_Shopping>("mobile-bff")
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(basketApi)
    // .WithReference(identityApi) // Identity disabled
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);

// Apps
var webhooksClient = builder.AddProject<Projects.WebhookClient>("webhooksclient", launchProfileName)
    .WithReference(webHooksApi)
    // .WithEnvironment("IdentityUrl", identityEndpoint) // Identity disabled
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);

var webApp = builder.AddProject<Projects.WebApp>("webapp", launchProfileName)
    .WithExternalHttpEndpoints()
    .WithUrls(c => c.Urls.ForEach(u => u.DisplayText = $"Online Store ({u.Endpoint?.EndpointName})"))
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    // .WithEnvironment("IdentityUrl", identityApi.GetEndpoint("http")) // Identity disabled
    .WithEnvironment("DisableAuth", "true")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otlpHeaders);

// set to true if you want to use OpenAI
bool useOpenAI = false;
if (useOpenAI)
{
    builder.AddOpenAI(catalogApi, webApp);
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
// Identity API disabled
// identityApi.WithEnvironment("BasketApiClient", basketApi.GetEndpoint("http"))
//            .WithEnvironment("OrderingApiClient", orderingApi.GetEndpoint("http"))
//            .WithEnvironment("WebhooksApiClient", webHooksApi.GetEndpoint("http"))
//            .WithEnvironment("WebhooksWebClient", webhooksClient.GetEndpoint(launchProfileName))
//            .WithEnvironment("WebAppClient", "http://localhost:30509");

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
