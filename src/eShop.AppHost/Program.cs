using eShop.AppHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddForwardedHeaders();

// Add Prometheus for metrics collection
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml", 
              "--storage.tsdb.path=/prometheus/", 
              "--web.console.libraries=/etc/prometheus/console_libraries",
              "--web.console.templates=/etc/prometheus/consoles",
              "--web.enable-lifecycle")
    .WithEndpoint(9090, 9090, "http")
    .WithExternalHttpEndpoints()
    .WithVolume("prometheus-data", "/prometheus")
    .WithLifetime(ContainerLifetime.Persistent);

// Add Grafana with Prometheus datasource configured  
var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin123")
    .WithEnvironment("GF_INSTALL_PLUGINS", "grafana-piechart-panel")
    .WithEndpoint(3000, 3000, "http")
    .WithExternalHttpEndpoints()
    .WithVolume("grafana-data", "/var/lib/grafana")
    .WithBindMount("./grafana-datasources.yml", "/etc/grafana/provisioning/datasources/datasources.yml")
    .WithBindMount("./grafana-dashboards.yml", "/etc/grafana/provisioning/dashboards/dashboards.yml")
    .WithBindMount("./k6-dashboard.json", "/etc/grafana/dashboards/k6-dashboard.json")
    .WithLifetime(ContainerLifetime.Persistent);

var redis = builder.AddRedis("redis");
var rabbitMq = builder.AddRabbitMQ("eventbus")
    .WithLifetime(ContainerLifetime.Persistent);
var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "md5")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=md5 --auth-local=md5");

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
    .WithEnvironment("DisableAuth", "true");
redis.WithParentRelationship(basketApi);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(catalogDb)
    .WithEnvironment("DisableAuth", "true")
    .WithExternalHttpEndpoints();

var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb).WaitFor(orderDb)
    .WithHttpHealthCheck("/health")
    // .WithEnvironment("Identity__Url", identityEndpoint) // Identity disabled
    .WithEnvironment("DisableAuth", "true");

builder.AddProject<Projects.OrderProcessor>("order-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb)
    .WaitFor(orderingApi) // wait for the orderingApi to be ready because that contains the EF migrations
    .WithEnvironment("DisableAuth", "true");

builder.AddProject<Projects.PaymentProcessor>("payment-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithEnvironment("DisableAuth", "true");

var webHooksApi = builder.AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(webhooksDb)
    // .WithEnvironment("Identity__Url", identityEndpoint) // Identity disabled
    .WithEnvironment("DisableAuth", "true");

// Reverse proxies
builder.AddProject<Projects.Mobile_Bff_Shopping>("mobile-bff")
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(basketApi)
    // .WithReference(identityApi) // Identity disabled
    .WithEnvironment("DisableAuth", "true");

// Apps
var webhooksClient = builder.AddProject<Projects.WebhookClient>("webhooksclient", launchProfileName)
    .WithReference(webHooksApi)
    // .WithEnvironment("IdentityUrl", identityEndpoint) // Identity disabled
    .WithEnvironment("DisableAuth", "true");

var webApp = builder.AddProject<Projects.WebApp>("webapp", launchProfileName)
    .WithExternalHttpEndpoints()
    .WithUrls(c => c.Urls.ForEach(u => u.DisplayText = $"Online Store ({u.Endpoint?.EndpointName})"))
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    // .WithEnvironment("IdentityUrl", identityApi.GetEndpoint("http")) // Identity disabled
    .WithEnvironment("DisableAuth", "true");

// set to true if you want to use OpenAI
bool useOpenAI = false;
if (useOpenAI)
{
    builder.AddOpenAI(catalogApi, webApp, OpenAITarget.OpenAI); // set to AzureOpenAI if you want to use Azure OpenAI
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
