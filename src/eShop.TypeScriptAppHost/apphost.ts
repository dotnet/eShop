// eShop Aspire TypeScript AppHost
// Translated from eShop.AppHost/Program.cs
// For more information, see: https://aspire.dev

import { createBuilder, ContainerLifetime, QueryParameterMatchMode } from './.modules/aspire.js';

const builder = await createBuilder();

builder.addAzureContainerAppEnvironment("env");

// Infrastructure
const redis = await builder.addRedis("redis");
const rabbitMq = await builder.addRabbitMQ("eventbus")
    .withLifetime(ContainerLifetime.Persistent);
const postgres = await builder.addPostgres("postgres")
    .withImage("ankane/pgvector", { tag: "latest" })
    .withLifetime(ContainerLifetime.Persistent);

const catalogDb = await postgres.addDatabase("catalogdb");
const identityDb = await postgres.addDatabase("identitydb");
const orderDb = await postgres.addDatabase("orderingdb");
const webhooksDb = await postgres.addDatabase("webhooksdb");

// For test use only.
// Looks for an environment variable that forces the use of HTTP for all the endpoints.
// We are doing this for ease of running the Playwright tests in CI.
function shouldUseHttpForEndpoints(): boolean {
    const envValue = process.env["ESHOP_USE_HTTP_ENDPOINTS"];
    return envValue === "1";
}

const launchProfileName = shouldUseHttpForEndpoints() ? "http" : "https";

// Services
const identityApi = await builder.addProject("identity-api", "../Identity.API/Identity.API.csproj", launchProfileName)
    .withExternalHttpEndpoints()
    .withReference(identityDb)
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

const identityEndpoint = await identityApi.getEndpoint(launchProfileName);

const basketApi = await builder.addCSharpApp("basket-api", "../Basket.API/Basket.API.csproj")
    .withReference(redis)
    .withReference(rabbitMq).waitFor(rabbitMq)
    .withEnvironmentEndpoint("Identity__Url", identityEndpoint)
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");
redis.withParentRelationship(basketApi);

const catalogApi = await builder.addCSharpApp("catalog-api", "../Catalog.API/Catalog.API.csproj")
    .withReference(rabbitMq).waitFor(rabbitMq)
    .withReference(catalogDb)
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

const orderingApi = await builder.addCSharpApp("ordering-api", "../Ordering.API/Ordering.API.csproj")
    .withReference(rabbitMq).waitFor(rabbitMq)
    .withReference(orderDb).waitFor(orderDb)
    .withHttpHealthCheck({ path: "/health" })
    .withEnvironmentEndpoint("Identity__Url", identityEndpoint)
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

await builder.addCSharpApp("order-processor", "../OrderProcessor/OrderProcessor.csproj")
    .withReference(rabbitMq).waitFor(rabbitMq)
    .withReference(orderDb)
    .waitFor(orderingApi) // wait for the orderingApi because it contains the EF migrations
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

await builder.addCSharpApp("payment-processor", "../PaymentProcessor/PaymentProcessor.csproj")
    .withReference(rabbitMq).waitFor(rabbitMq)
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

const webHooksApi = await builder.addCSharpApp("webhooks-api", "../Webhooks.API/Webhooks.API.csproj")
    .withReference(rabbitMq).waitFor(rabbitMq)
    .withReference(webhooksDb)
    .withEnvironmentEndpoint("Identity__Url", identityEndpoint)
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

// Reverse proxies
await builder.addYarp("mobile-bff")
    .withExternalHttpEndpoints()
    .withConfiguration(async (yarp) => {
        const catalogCluster = await yarp.addClusterFromResource(catalogApi);

        await yarp.addRoute("/catalog-api/api/catalog/items", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1", "2.0"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/items/by", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1", "2.0"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/items/{id}", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1", "2.0"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/items/by/{name}", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/items/withsemanticrelevance/{text}", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/items/withsemanticrelevance", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["2.0"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/items/type/{typeId}/brand/{brandId?}", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/items/type/all/brand/{brandId?}", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/catalogTypes", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1", "2.0"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/catalogBrands", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1", "2.0"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        await yarp.addRoute("/catalog-api/api/catalog/items/{id}/pic", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1", "2.0"], mode: QueryParameterMatchMode.Exact }])
            .withTransformPathRemovePrefix("/catalog-api");

        // Generic catalog catch-all route
        await yarp.addRoute("/api/catalog/{*any}", catalogCluster)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1", "2.0"], mode: QueryParameterMatchMode.Exact }]);

        // Ordering routes
        const orderingEndpoint = await orderingApi.getEndpoint("http");
        await yarp.addRouteFromEndpoint("/api/orders/{*any}", orderingEndpoint)
            .withMatchRouteQueryParameter([{ name: "api-version", values: ["1.0", "1"], mode: QueryParameterMatchMode.Exact }]);

        // Identity routes
        const identityHttpEndpoint = await identityApi.getEndpoint("http");
        await yarp.addRouteFromEndpoint("/identity/{*any}", identityHttpEndpoint)
            .withTransformPathRemovePrefix("/identity");
    });

// Apps
const webhooksClient = await builder.addProject("webhooksclient", "../WebhookClient/WebhookClient.csproj", launchProfileName)
    .withReference(webHooksApi)
    .withEnvironmentEndpoint("IdentityUrl", identityEndpoint)
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

const webApp = await builder.addProject("webapp", "../WebApp/WebApp.csproj", launchProfileName)
    .withExternalHttpEndpoints()
    .withUrlForEndpoint("http", async (url) => { url.displayText = "Online Store (http)"; })
    .withUrlForEndpoint("https", async (url) => { url.displayText = "Online Store (https)"; })
    .withReference(basketApi)
    .withReference(catalogApi)
    .withReference(orderingApi)
    .withReference(rabbitMq).waitFor(rabbitMq)
    .withEnvironmentEndpoint("IdentityUrl", identityEndpoint)
    .withEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

// Set to true if you want to use OpenAI
const useOpenAI = true;
if (useOpenAI) {
    const openAI = await builder.addAzureOpenAI("openai");

    const chat = await openAI.addDeployment("chatModel", "gpt-4.1-mini", "2025-04-14")
        .withProperties(async (d) =>
        {
            d.deploymentName.set("gpt-4.1-mini");
            d.skuName.set("GlobalStandard");
            d.skuCapacity.set(50);
        });
    const textEmbedding = await openAI.addDeployment("textEmbeddingModel", "text-embedding-3-small", "1")
        .withProperties(async (d) =>
        {
            d.deploymentName.set("text-embedding-3-small");
            d.skuCapacity.set(20);
        });

    catalogApi.withReference(textEmbedding);
    webApp.withReference(chat);
}

// Ollama configuration (disabled by default)
// NOTE: CommunityToolkit Ollama integration is not yet available in the TypeScript SDK.
// Set to true if you want to use Ollama (requires manual integration)
const useOllama = false;
if (useOllama) {
    // TODO: addOllama is not available in the TypeScript SDK yet.
    // In C#: builder.AddOllama("ollama").WithDataVolume().WithGPUSupport().WithOpenWebUI()
    // const embeddings = ollama.AddModel("embedding", "all-minilm");
    // const chat = ollama.AddModel("chat", "llama3.1");
}

// Wire up the callback urls (self referencing)
const webAppEndpoint = await webApp.getEndpoint(launchProfileName);
const webhooksClientEndpoint = await webhooksClient.getEndpoint(launchProfileName);
webApp.withEnvironmentEndpoint("CallBackUrl", webAppEndpoint);
webhooksClient.withEnvironmentEndpoint("CallBackUrl", webhooksClientEndpoint);

// Identity has a reference to all of the apps for callback urls, this is a cyclic reference
const basketHttpEndpoint = await basketApi.getEndpoint("http");
const orderingHttpEndpoint = await orderingApi.getEndpoint("http");
const webHooksHttpEndpoint = await webHooksApi.getEndpoint("http");
identityApi
    .withEnvironmentEndpoint("BasketApiClient", basketHttpEndpoint)
    .withEnvironmentEndpoint("OrderingApiClient", orderingHttpEndpoint)
    .withEnvironmentEndpoint("WebhooksApiClient", webHooksHttpEndpoint)
    .withEnvironmentEndpoint("WebhooksWebClient", webhooksClientEndpoint)
    .withEnvironmentEndpoint("WebAppClient", webAppEndpoint);

await builder.build().run();