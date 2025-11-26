using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Yarp;
using Aspire.Hosting.Yarp.Transforms;
using Yarp.ReverseProxy.Configuration;

namespace eShop.AppHost;

internal enum OpenAITarget
{
    OpenAI,
    AzureOpenAI,
    AzureOpenAIExisting,
    AzureOpenAIExistingWithKey
}

internal static class Extensions
{
    /// <summary>
    /// Adds a hook to set the ASPNETCORE_FORWARDEDHEADERS_ENABLED environment variable to true for all projects in the application.
    /// </summary>
    public static IDistributedApplicationBuilder AddForwardedHeaders(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddEventingSubscriber<AddForwardHeadersSubscriber>();
        return builder;
    }

    private class AddForwardHeadersSubscriber : IDistributedApplicationEventingSubscriber
    {
        public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
            {
                foreach (var p in @event.Model.GetProjectResources())
                {
                    p.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                    {
                        context.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                    }));
                }

                return Task.CompletedTask;
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Configures eShop projects to use OpenAI for text embedding and chat.
    /// </summary>
    public static IDistributedApplicationBuilder AddOpenAI(this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> catalogApi,
        IResourceBuilder<ProjectResource> webApp,
        OpenAITarget openAITarget)
    {
        const string openAIName = "openai";

        const string textEmbeddingName = "textEmbeddingModel";
        const string textEmbeddingModelName = "text-embedding-3-small";

        const string chatName = "chatModel";
        const string chatModelName = "gpt-4.1-mini";

        if (openAITarget != OpenAITarget.AzureOpenAI)
        {
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            IResourceBuilder<ParameterResource>? endpoint = null;
            if (openAITarget != OpenAITarget.OpenAI)
            {
                endpoint = builder.AddParameter("OpenAIEndpointParameter")
                    .WithDescription("The Azure OpenAI endpoint to use, e.g. https://<name>.openai.azure.com/")
                    .WithCustomInput(p => new()
                    {
                        Name = "OpenAIEndpointParameter",
                        Label = "Azure OpenAI Endpoint",
                        InputType = InputType.Text,
                        Value = "https://<name>.openai.azure.com/",
                    });
            }

            IResourceBuilder<ParameterResource>? key = null;
            if (openAITarget is OpenAITarget.OpenAI or OpenAITarget.AzureOpenAIExistingWithKey)
            {
                key = builder.AddParameter("OpenAIKeyParameter", secret: true)
                    .WithDescription("The OpenAI API key to use.")
                    .WithCustomInput(p => new()
                    {
                        Name = "OpenAIKeyParameter",
                        Label = "API Key",
                        InputType = InputType.SecretText
                    });
            }

            var chatModel = builder.AddParameter("ChatModelParameter")
                .WithDescription("The chat model to use.")
                .WithCustomInput(p => new()
                {
                    Name = "ChatModelParameter",
                    Label = "Chat Model",
                    InputType = InputType.Text,
                    Value = chatModelName,
                });

            var embeddingModel = builder.AddParameter("EmbeddingModelParameter")
                .WithDescription("The embedding model to use.")
                .WithCustomInput(p => new()
                {
                    Name = "EmbeddingModelParameter",
                    Label = "Text Embedding Model",
                    InputType = InputType.Text,
                    Value = textEmbeddingModelName,
                });
#pragma warning restore ASPIREINTERACTION001

            var openAIConnectionBuilder = new ReferenceExpressionBuilder();
            if (endpoint is not null)
            {
                openAIConnectionBuilder.Append($"Endpoint={endpoint}");
            }
            if (key is not null)
            {
                openAIConnectionBuilder.Append($";Key={key}");
            }
            var openAIConnectionString = openAIConnectionBuilder.Build();

            catalogApi.WithReference(builder.AddConnectionString(textEmbeddingName, cs =>
            {
                cs.Append($"{openAIConnectionString};Deployment={embeddingModel}");
            }));
            webApp.WithReference(builder.AddConnectionString(chatName, cs =>
            {
                cs.Append($"{openAIConnectionString};Deployment={chatModel}");
            }));
        }
        else
        {
            var openAI = builder.AddAzureOpenAI(openAIName);

            var chat = openAI.AddDeployment(chatName, chatModelName, "2025-04-14")
                .WithProperties(d =>
                {
                    d.DeploymentName = chatModelName;
                    d.SkuName = "GlobalStandard";
                    d.SkuCapacity = 50;
                });
            var textEmbedding = openAI.AddDeployment(textEmbeddingName, textEmbeddingModelName, "1")
                .WithProperties(d =>
                {
                    d.DeploymentName = textEmbeddingModelName;
                    d.SkuCapacity = 20; // 20k tokens per minute are needed to seed the initial embeddings
                });

            catalogApi.WithReference(textEmbedding);
            webApp.WithReference(chat);
        }

        return builder;
    }

    /// <summary>
    /// Configures eShop projects to use Ollama for text embedding and chat.
    /// </summary>
    public static IDistributedApplicationBuilder AddOllama(this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> catalogApi,
        IResourceBuilder<ProjectResource> webApp)
    {
        var ollama = builder.AddOllama("ollama")
            .WithDataVolume()
            .WithGPUSupport()
            .WithOpenWebUI();
        var embeddings = ollama.AddModel("embedding", "all-minilm");
        var chat = ollama.AddModel("chat", "llama3.1");

        catalogApi.WithReference(embeddings)
            .WithEnvironment("OllamaEnabled", "true")
            .WaitFor(embeddings);
        webApp.WithReference(chat)
            .WithEnvironment("OllamaEnabled", "true")
            .WaitFor(chat);

        return builder;
    }

    public static IResourceBuilder<YarpResource> ConfigureMobileBffRoutes(this IResourceBuilder<YarpResource> builder,
        IResourceBuilder<ProjectResource> catalogApi,
        IResourceBuilder<ProjectResource> orderingApi,
        IResourceBuilder<ProjectResource> identityApi)
    {
        return builder.WithConfiguration(yarp =>
        {
            var catalogCluster = yarp.AddCluster(catalogApi);

            yarp.AddRoute("/catalog-api/api/catalog/items", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/items/by", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/items/{id}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/items/by/{name}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/items/withsemanticrelevance/{text}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/items/withsemanticrelevance", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/items/type/{typeId}/brand/{brandId?}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/items/type/all/brand/{brandId?}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/catalogTypes", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/catalogBrands", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            yarp.AddRoute("/catalog-api/api/catalog/items/{id}/pic", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // Generic catalog catch-all route
            yarp.AddRoute("/api/catalog/{*any}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }]);

            // Ordering routes
            yarp.AddRoute("/api/orders/{*any}", orderingApi.GetEndpoint("http"))
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }]);

            // Identity routes
            yarp.AddRoute("/identity/{*any}", identityApi.GetEndpoint("http"))
                .WithTransformPathRemovePrefix("/identity");
        });
    }
}
