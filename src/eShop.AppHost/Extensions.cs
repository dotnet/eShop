using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;

namespace eShop.AppHost;

internal static class Extensions
{
    /// <summary>
    /// Adds a hook to set the ASPNETCORE_FORWARDEDHEADERS_ENABLED environment variable to true for all projects in the application.
    /// </summary>
    public static IDistributedApplicationBuilder AddForwardedHeaders(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddLifecycleHook<AddForwardHeadersHook>();
        return builder;
    }

    private class AddForwardHeadersHook : IDistributedApplicationLifecycleHook
    {
        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            foreach (var p in appModel.GetProjectResources())
            {
                p.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                {
                    context.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                }));
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Configures eShop projects to use OpenAI for text embedding and chat.
    /// </summary>
    public static IDistributedApplicationBuilder AddOpenAI(this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> catalogApi,
        IResourceBuilder<ProjectResource> webApp)
    {
        const string openAIName = "openai";

        const string textEmbeddingName = "textEmbeddingModel";
        const string textEmbeddingModelName = "text-embedding-3-small";

        const string chatName = "chatModel";
        const string chatModelName = "gpt-4.1-mini";

        // to use an existing OpenAI resource as a connection string, add the following to the AppHost user secrets:
        // "ConnectionStrings": {
        //   "openai": "Key=<API Key>" (to use https://api.openai.com/)
        //     -or-
        //   "openai": "Endpoint=https://<name>.openai.azure.com/" (to use Azure OpenAI)
        // }
        if (builder.Configuration.GetConnectionString(openAIName) is string openAIConnectionString)
        {
            catalogApi.WithReference(
                builder.AddConnectionString(textEmbeddingName, ReferenceExpression.Create($"{openAIConnectionString};Deployment={textEmbeddingModelName}")));
            webApp.WithReference(
                builder.AddConnectionString(chatName, ReferenceExpression.Create($"{openAIConnectionString};Deployment={chatModelName}")));
        }
        else
        {
            // to use Azure provisioning, add the following to the AppHost user secrets:
            // "Azure": {
            //   "SubscriptionId": "<your subscription ID>",
            //   "ResourceGroupPrefix": "<prefix>",
            //   "Location": "<location>"
            // }

            var openAI = builder.AddAzureOpenAI(openAIName);

            // to use an existing Azure OpenAI resource via provisioning, add the following to the AppHost user secrets:
            // "Parameters": {
            //   "openaiName": "<Azure OpenAI resource name>",
            //   "openaiResourceGroup": "<Azure OpenAI resource group>"
            // }
            // - or -
            // leave the parameters out to create a new Azure OpenAI resource
            if (builder.Configuration["Parameters:openaiName"] is not null &&
                builder.Configuration["Parameters:openaiResourceGroup"] is not null)
            {
                openAI.AsExisting(
                    builder.AddParameter("openaiName"),
                    builder.AddParameter("openaiResourceGroup"));
            }

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
}
