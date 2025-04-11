using inked.AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddForwardedHeaders();

var redis = builder.AddRedis("redis").WithRedisCommander();
var rabbitMq = builder.AddRabbitMQ("eventbus")
    .WithLifetime(ContainerLifetime.Persistent);
var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");
var submissionDb = postgres.AddDatabase("submissiondb");

var launchProfileName = ShouldUseHttpForEndpoints() ? "http" : "https";

// Services
var username = builder.AddParameter("username");
var password = builder.AddParameter("password", true);
var keycloak = builder.AddKeycloak("keycloak", 8080, username, password)
    .WithDataVolume()
    .WithEnvironment("GOOGLE_CLIENT_ID", Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"))
    .WithEnvironment("GOOGLE_CLIENT_SECRET", Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET"));

var identityEndpoint = keycloak.GetEndpoint("http");

var basketApi = builder.AddProject<Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("Identity__Url", identityEndpoint);

redis.WithParentRelationship(basketApi);

var catalogApi = builder.AddProject<Catalog_API>("catalog-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(catalogDb)
    .WithParentRelationship(basketApi)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("Identity__Url", identityEndpoint);

var submissionApi = builder.AddProject<Submission_API>("submission-api", launchProfileName)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(submissionDb)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithExternalHttpEndpoints()
    .WithEnvironment("Identity__Url", identityEndpoint);

var orderingApi = builder.AddProject<Ordering_API>("ordering-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb).WaitFor(orderDb)
    .WithHttpHealthCheck("/health")
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("Identity__Url", identityEndpoint);

builder.AddProject<OrderProcessor>("order-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb)
    .WaitFor(orderingApi); // wait for the orderingApi to be ready because that contains the EF migrations

builder.AddProject<PaymentProcessor>("payment-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq);

builder.AddProject<RefundProcessor>("refund-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq);

var webHooksApi = builder.AddProject<Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(webhooksDb)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithEnvironment("Identity__Url", identityEndpoint);

// Reverse proxies
builder.AddProject<Mobile_Bff_Shopping>("mobile-bff")
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(basketApi)
    .WithReference(keycloak)
    .WithReference(submissionApi);

// Apps
var webhooksClient = builder.AddProject<WebhookClient>("webhooksclient", launchProfileName)
    .WithReference(webHooksApi)
    .WithReference(keycloak)
    .WithEnvironment("Identity__Url", identityEndpoint);

var webApp = builder.AddProject<WebApp>("webapp", launchProfileName)
    .WithExternalHttpEndpoints()
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(submissionApi).WaitFor(submissionApi)
    .WithReference(orderingApi).WaitFor(orderingApi)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(keycloak)
    .WithEnvironment("Identity__Url", identityEndpoint);

keycloak.WithReference(basketApi)
    .WithReference(webHooksApi)
    .WithReference(submissionApi)
    .WithReference(orderingApi)
    .WithReference(catalogApi)
    .WithReference(webApp)
    .WithEnvironment("Identity__Url", identityEndpoint);

webApp.WithEnvironment("Identity__Callback", webApp.GetEndpoint(launchProfileName));
// set to true if you want to use OpenAI
var useOpenAI = false;
if (useOpenAI)
{
    builder.AddOpenAI(catalogApi, webApp);
}

var useOllama = false;
if (useOllama)
{
    builder.AddOllama(catalogApi, webApp);
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
    return int.TryParse(envValue, out var result) && result == 1;
}
