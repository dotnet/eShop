using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class CodespaceExtensions
{
    public static IDistributedApplicationBuilder WithCodespaces(this IDistributedApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue<bool>("CODESPACES"))
        {
            return builder;
        }

        builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
        {
            _ = Task.Run(() => UrlRewriterAsync(e.Services, ct));
            return Task.CompletedTask;
        });

        return builder;
    }

    private static async Task UrlRewriterAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var gitHubCodespacesPortForwardingDomain = configuration.GetValue<string>("GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN") ?? throw new DistributedApplicationException("Codespaces was detected but GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN environment missing.");
        var codespaceName = configuration.GetValue<string>("CODESPACE_NAME") ?? throw new DistributedApplicationException("Codespaces was detected but CODESPACE_NAME environment missing.");

        var rns = services.GetRequiredService<ResourceNotificationService>();

        var resourceEvents = rns.WatchAsync(cancellationToken);

        await foreach (var resourceEvent in resourceEvents)
        {
            Dictionary<UrlSnapshot, UrlSnapshot>? remappedUrls = null;

            foreach (var originalUrlSnapshot in resourceEvent.Snapshot.Urls)
            {
                var uri = new Uri(originalUrlSnapshot.Url);

                if (!originalUrlSnapshot.IsInternal && (uri.Scheme == "http" || uri.Scheme == "https") && uri.Host == "localhost")
                {
                    if (remappedUrls is null)
                    {
                        remappedUrls = new();
                    }

                    var newUrlSnapshot = originalUrlSnapshot with
                    {
                        Url = $"{uri.Scheme}://{codespaceName}-{uri.Port}.{gitHubCodespacesPortForwardingDomain}{uri.AbsolutePath}"
                    };

                    remappedUrls.Add(originalUrlSnapshot, newUrlSnapshot);
                }
            }

            if (remappedUrls is not null)
            {
                var transformedUrls = from originalUrl in resourceEvent.Snapshot.Urls
                                      select remappedUrls.TryGetValue(originalUrl, out var remappedUrl) ? remappedUrl : originalUrl;

                await rns.PublishUpdateAsync(resourceEvent.Resource, resourceEvent.ResourceId, s => s with
                {
                    Urls = transformedUrls.ToImmutableArray()
                });
            }
        }
    }
}
