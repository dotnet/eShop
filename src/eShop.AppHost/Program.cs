var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedisContainer("redis");
var rabbitMq = builder.AddRabbitMQContainer("EventBus");
var postgres = builder.AddPostgresContainer("postgres")
    .WithAnnotation(new ContainerImageAnnotation
    {
        Image = "ankane/pgvector",
        Tag = "latest"
    });

var catalogDb = postgres.AddDatabase("CatalogDB");
var identityDb = postgres.AddDatabase("IdentityDB");
var orderDb = postgres.AddDatabase("OrderingDB");
var webhooksDb = postgres.AddDatabase("WebHooksDB");

// Services
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb);

var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WithEnvironmentForServiceBinding("Identity__Url", identityApi);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq)
    .WithReference(catalogDb);

var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq)
    .WithReference(orderDb)
    .WithEnvironmentForServiceBinding("Identity__Url", identityApi);

builder.AddProject<Projects.Ordering_BackgroundTasks>("order-processor")
    .WithReference(rabbitMq)
    .WithReference(orderDb);

builder.AddProject<Projects.Payment_API>("payment-processor")
    .WithReference(rabbitMq);

var webHooksApi = builder.AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq)
    .WithReference(webhooksDb)
    .WithEnvironmentForServiceBinding("Identity__Url", identityApi);

// Reverse proxies
builder.AddProject<Projects.Mobile_Bff_Shopping>("mobile-bff")
    .WithReference(catalogApi)
    .WithReference(identityApi);

// Apps
var webhooksClient = builder.AddProject<Projects.WebhookClient>("webhooksclient")
    .WithReference(webHooksApi)
    .WithEnvironmentForServiceBinding("IdentityUrl", identityApi);

var webApp = builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq)
    .WithEnvironmentForServiceBinding("IdentityUrl", identityApi)
    .WithLaunchProfile("https");

// Wire up the callback urls (self referencing)
webApp.WithEnvironmentForServiceBinding("CallBackUrl", webApp, bindingName: "https");
webhooksClient.WithEnvironmentForServiceBinding("CallBackUrl", webhooksClient);

// Identity has a reference to all of the apps for callback urls, this is a cyclic reference
identityApi.WithEnvironmentForServiceBinding("BasketApiClient", basketApi)
           .WithEnvironmentForServiceBinding("OrderingApiClient", orderingApi)
           .WithEnvironmentForServiceBinding("WebhooksWebClient", webhooksClient)
           .WithEnvironmentForServiceBinding("WebhooksApiClient", webHooksApi)
           .WithEnvironmentForServiceBinding("WebAppClient", webApp, bindingName: "https");

builder.Build().Run();
