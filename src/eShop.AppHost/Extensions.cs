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
        const string textEmbeddingAzureDeployName = "text-embedding-3-small"; // unique identifier for specific deployment within Azure OpenAI resource
        const string textEmbeddingModelName = textEmbeddingAzureDeployName; // actual OpenAI model used in deployment
        const string chatModelAzureDeployName = "gpt-4o-mini";
        const string chatModelName = chatModelAzureDeployName;

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
            //   "SubscriptionId": "<your subscription ID>",
            //   "ResourceGroupPrefix": "<prefix>",
            //   "Location": "<location>"
            // }
            openAI = builder.AddAzureOpenAI(openAIName)
                .AddDeployment(new AzureOpenAIDeployment(chatModelAzureDeployName, chatModelName, "2024-07-18"))
                .AddDeployment(new AzureOpenAIDeployment(textEmbeddingAzureDeployName, textEmbeddingModelName, "1", skuCapacity: 20)); // 20k tokens per minute are needed to seed the initial embeddings
        }

        catalogApi
            .WithReference(openAI)
            .WithEnvironment("AI__OPENAI__EMBEDDINGMODEL", textEmbeddingModelName);

        webApp
            .WithReference(openAI)
            .WithEnvironment("AI__OPENAI__CHATMODEL", chatModelName);

        return builder;
    }
}
