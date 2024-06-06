using eShop.AppHost;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddForwardedHeaders();

var redis = builder.AddRedis("redis");
var rabbitMq = builder.AddRabbitMQ("eventbus");
var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest");

var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");

var launchProfileName = ShouldUseHttpForEndpoints() ? "http" : "https";

// Services
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api", launchProfileName)
    .WithExternalHttpEndpoints()
    .WithReference(identityDb);

var identityEndpoint = identityApi.GetEndpoint(launchProfileName);

var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WithEnvironment("Identity__Url", identityEndpoint);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq)
    .WithReference(catalogDb);

var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq)
    .WithReference(orderDb)
    .WithEnvironment("Identity__Url", identityEndpoint);

builder.AddProject<Projects.OrderProcessor>("order-processor")
    .WithReference(rabbitMq)
    .WithReference(orderDb);

builder.AddProject<Projects.PaymentProcessor>("payment-processor")
    .WithReference(rabbitMq);

var webHooksApi = builder.AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq)
    .WithReference(webhooksDb)
    .WithEnvironment("Identity__Url", identityEndpoint);

// Reverse proxies
builder.AddProject<Projects.Mobile_Bff_Shopping>("mobile-bff")
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(basketApi)
    .WithReference(identityApi);

// Apps
var webhooksClient = builder.AddProject<Projects.WebhookClient>("webhooksclient", launchProfileName)
    .WithReference(webHooksApi)
    .WithEnvironment("IdentityUrl", identityEndpoint);

var webApp = builder.AddProject<Projects.WebApp>("webapp", launchProfileName)
    .WithExternalHttpEndpoints()
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq)
    .WithEnvironment("IdentityUrl", identityEndpoint);

// set to true if you want to use OpenAI
bool useOpenAI = false;
if (useOpenAI)
{
    const string openAIName = "openai";
    const string textEmbeddingName = "text-embedding-3-small";
    const string chatModelName = "gpt-35-turbo-16k";

    // to use an existing OpenAI resource, add the following to the AppHost user secrets:
    // "ConnectionStrings": {
    //   "openai": "Key=<API Key>" (to use https://api.openai.com/)
    //     -or-
    //   "openai": "Endpoint=https://<name>.openai.azure.com/" (to use Azure OpenAI)
    // }
    IResourceBuilder<IResourceWithConnectionString> openAI;
    if (builder.Configuration.GetConnectionString(openAIName) is not null)
    {
        openAI = builder.AddConnectionString(openAIName);
    }
    else
    {
        // to use Azure provisioning, add the following to the AppHost user secrets:
        // "Azure": {
        //   "SubscriptionId": "<your subscription ID>"
        //   "Location": "<location>"
        // }
        openAI = builder.AddAzureOpenAI(openAIName)
            .AddDeployment(new AzureOpenAIDeployment(chatModelName, "gpt-35-turbo", "0613"))
            .AddDeployment(new AzureOpenAIDeployment(textEmbeddingName, "text-embedding-3-small", "1"));
    }

    catalogApi
        .WithReference(openAI)
        .WithEnvironment("AI__OPENAI__EMBEDDINGNAME", textEmbeddingName);

    webApp
        .WithReference(openAI)
        .WithEnvironment("AI__OPENAI__CHATMODEL", chatModelName); ;
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
