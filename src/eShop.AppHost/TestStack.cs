using System.Runtime.InteropServices;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;

namespace eShop.AppHost;

/// <summary>
/// Docker Compose "test stack" used for reproducible, scenario-driven test runs.
/// Enabled with the <c>TestStack=true</c> configuration value (for example via <c>testing/teststack publish</c>).
/// Only compose-level customizations live here; the resource graph itself is shared with production.
/// </summary>
internal static class TestStack
{
    public const string EnvironmentName = "teststack";

    // Host name that resolves to the Docker host both from containers (Docker Desktop / host-gateway)
    // and from test browsers (mapped to 127.0.0.1, see playwright.teststack.config.ts). OIDC requires the
    // browser and the services to see the same identity URL, just like the external FQDN in Azure.
    public const string PublicHost = "host.docker.internal";

    public const int WebAppPort = 18080;
    public const int IdentityApiPort = 18081;
    public const int CatalogApiPort = 18082;
    public const int BasketApiPort = 18083;
    public const int OrderingApiPort = 18084;
    public const int WebhooksApiPort = 18085;
    public const int WebhooksClientPort = 18086;
    public const int MobileBffPort = 18087;
    public const int DashboardPort = 18888;
    public const int PostgresPort = 18432;
    public const int RedisPort = 18379;
    public const int RabbitMqPort = 18672;

    public static string IdentityUrl => $"http://{PublicHost}:{IdentityApiPort}";
    public static string WebAppUrl => $"http://localhost:{WebAppPort}";
    public static string WebhooksClientUrl => $"http://localhost:{WebhooksClientPort}";

    public static bool IsEnabled(IConfiguration configuration) =>
        bool.TryParse(configuration["TestStack"], out var enabled) && enabled;

    public static IResourceBuilder<DockerComposeEnvironmentResource> AddTestStackEnvironment(this IDistributedApplicationBuilder builder) =>
        builder.AddDockerComposeEnvironment(EnvironmentName)
            .WithDashboard(dashboard => dashboard.WithHostPort(DashboardPort))
            .ConfigureComposeFile(file => file.Name = "eshop-teststack");

    /// <summary>
    /// Publishes a fixed host port for the resource's primary endpoint, lets the service reach the Docker host,
    /// applies browser-facing URL overrides and, for services with built-in seeders, turns the seeders off so
    /// scenario files own all data (EF migrations still run at startup, as in production).
    /// </summary>
    public static IResourceBuilder<T> WithTestStackService<T>(
        this IResourceBuilder<T> builder,
        int? hostPort = null,
        int? containerPort = null,
        bool seeded = false,
        Dictionary<string, string>? env = null)
        where T : IComputeResource
    {
        if (builder.Resource.TryGetLastAnnotation<IProjectMetadata>(out _))
        {
            var platform = GetTargetPlatform(builder.ApplicationBuilder.Configuration["TestStackPlatform"]);
#pragma warning disable ASPIREPIPELINES003 // Container build options are in evaluation.
            builder.WithContainerBuildOptions(options => options.TargetPlatform = platform);
#pragma warning restore ASPIREPIPELINES003
        }

        return builder.PublishAsDockerComposeService((composeService, service) =>
        {
            if (hostPort is int port)
            {
                var targetPort = containerPort?.ToString() ?? GetPrimaryTargetPort(service);
                service.Ports = [$"{port}:{targetPort}"];
            }

            service.ExtraHosts[PublicHost] = "host-gateway";
            service.Restart = "unless-stopped";

            if (seeded)
            {
                service.Environment["Seeding__Disabled"] = "true";
            }

            foreach (var (name, value) in env ?? [])
            {
                service.Environment[name] = value;
            }
        });
    }

    // Images run on the developer's machine, so build them for its architecture to avoid emulation.
    // TestStackPlatform=linux-amd64 reproduces the production (Azure Container Apps) architecture exactly.
#pragma warning disable ASPIREPIPELINES003
    private static ContainerTargetPlatform GetTargetPlatform(string? configured) =>
        configured switch
        {
            "linux-amd64" => ContainerTargetPlatform.LinuxAmd64,
            "linux-arm64" => ContainerTargetPlatform.LinuxArm64,
            null or "" => RuntimeInformation.OSArchitecture == Architecture.Arm64
                ? ContainerTargetPlatform.LinuxArm64
                : ContainerTargetPlatform.LinuxAmd64,
            _ => throw new InvalidOperationException($"Unsupported TestStackPlatform '{configured}'. Use linux-amd64 or linux-arm64."),
        };
#pragma warning restore ASPIREPIPELINES003

    // Project resources get their container port assigned by the compose publisher (exposed via HTTP_PORTS).
    private static string GetPrimaryTargetPort(Aspire.Hosting.Docker.Resources.ComposeNodes.Service service)
    {
        if (service.Environment.TryGetValue("HTTP_PORTS", out var httpPorts) && !string.IsNullOrEmpty(httpPorts))
        {
            return httpPorts.Split(';', ',')[0];
        }

        var existing = service.Ports.Concat(service.Expose).FirstOrDefault()
            ?? throw new InvalidOperationException($"Cannot determine the container port for compose service '{service.Name}'.");
        return existing.Split(':')[^1];
    }
}
