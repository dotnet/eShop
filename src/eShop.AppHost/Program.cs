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
var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(rabbitMq);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq)
    .WithReference(catalogDb);

var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(rabbitMq)
    .WithReference(identityDb);

var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq)
    .WithReference(orderDb);

builder.AddProject<Projects.Ordering_BackgroundTasks>("order-processor")
    .WithReference(rabbitMq)
    .WithReference(orderDb);

builder.AddProject<Projects.Payment_API>("payment-processor")
    .WithReference(rabbitMq);

var webHooksApi = builder.AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq)
    .WithReference(webhooksDb);

// Reverse proxies
builder.AddProject<Projects.Mobile_Bff_Shopping>("mobile-bff")
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(identityApi);

// Apps
builder.AddProject<Projects.WebhookClient>("webhooksclient")
    .WithReference(webHooksApi);

builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq)
    .WithLaunchProfile("https");
    
builder.Build().Run();
