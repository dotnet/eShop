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

// Services
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api", "https")
    .WithReference(identityDb);

var idpHttps = identityApi.GetEndpoint("https");

var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WithEnvironment("Identity__Url", idpHttps);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq)
    .WithReference(catalogDb);

var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq)
    .WithReference(orderDb)
    .WithEnvironment("Identity__Url", idpHttps);

builder.AddProject<Projects.OrderProcessor>("order-processor")
    .WithReference(rabbitMq)
    .WithReference(orderDb);

builder.AddProject<Projects.PaymentProcessor>("payment-processor")
    .WithReference(rabbitMq);

var webHooksApi = builder.AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq)
    .WithReference(webhooksDb)
    .WithEnvironment("Identity__Url", idpHttps);

// Reverse proxies
builder.AddProject<Projects.Mobile_Bff_Shopping>("mobile-bff")
    .WithReference(catalogApi)
    .WithReference(identityApi);

// Apps
var webhooksClient = builder.AddProject<Projects.WebhookClient>("webhooksclient")
    .WithReference(webHooksApi)
    .WithEnvironment("IdentityUrl", idpHttps);

var webApp = builder.AddProject<Projects.WebApp>("webapp", "https")
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq)
    .WithEnvironment("IdentityUrl", idpHttps);

#if USE_AZURE_OPENAI

const string openAIName = "openai";

// to configure an existing OpenAI connection, set it in the ApHost's user secrets
IResourceBuilder<IResourceWithConnectionString> openAI;
string textEmbeddingName = "text-embedding-ada-002";
string chatModelName = "gpt-35-turbo-16k";
if (builder.Configuration.GetConnectionString(openAIName) is not null)
{
    openAI = builder.AddConnectionString(openAIName);
    if (builder.Configuration["AI:OpenAI:EmbeddingName"] is string embedName)
    {
        textEmbeddingName = embedName;
    }
    if (builder.Configuration["AI:OpenAI:ChatModel"] is string chatName)
    {
        chatModelName = chatName;
    }
}
else
{
    builder.AddAzureProvisioning();

    openAI = builder.AddAzureOpenAI(openAIName)
        .AddDeployment(new AzureOpenAIDeployment(chatModelName, "gpt-35-turbo", "0613"))
        .AddDeployment(new AzureOpenAIDeployment(textEmbeddingName, "text-embedding-ada-002", "2"));
}

catalogApi
    .WithReference(openAI)
    .WithEnvironment("AI__OPENAI__EMBEDDINGNAME", textEmbeddingName);

webApp
    .WithReference(openAI)
    .WithEnvironment("AI__OPENAI__CHATMODEL", chatModelName);

#endif

// Wire up the callback urls (self referencing)
webApp.WithEnvironment("CallBackUrl", webApp.GetEndpoint("https"));
webhooksClient.WithEnvironment("CallBackUrl", webhooksClient.GetEndpoint("https"));

// Identity has a reference to all of the apps for callback urls, this is a cyclic reference
identityApi.WithEnvironment("BasketApiClient", basketApi.GetEndpoint("http"))
           .WithEnvironment("OrderingApiClient", orderingApi.GetEndpoint("http"))
           .WithEnvironment("WebhooksApiClient", webHooksApi.GetEndpoint("http"))
           .WithEnvironment("WebhooksWebClient", webhooksClient.GetEndpoint("https"))
           .WithEnvironment("WebAppClient", webApp.GetEndpoint("https"));

builder.Build().Run();
