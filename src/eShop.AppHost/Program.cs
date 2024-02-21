using eShop.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddForwardedHeaders();

var redis = builder.AddRedisContainer("redis");
var rabbitMq = builder.AddRabbitMQContainer("eventbus");
var postgres = builder.AddPostgresContainer("postgres")
    .WithAnnotation(new ContainerImageAnnotation
    {
        Image = "ankane/pgvector",
        Tag = "latest"
    });

var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");

var openAi = builder.AddAzureOpenAI("openai");

// Services
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb)
    .WithLaunchProfile("https");

var idpHttps = identityApi.GetEndpoint("https");

var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WithEnvironment("Identity__Url", idpHttps);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq)
    .WithReference(catalogDb)
    .WithReference(openAi, optional: true);

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

var webApp = builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq)
    .WithReference(openAi, optional: true)
    .WithEnvironment("IdentityUrl", idpHttps)
    .WithLaunchProfile("https");

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
