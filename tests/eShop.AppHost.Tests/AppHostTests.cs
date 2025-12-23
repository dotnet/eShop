using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eShop.AppHost.Tests;

public class AppHostTests
{
    [Fact]
    public async Task AllResourcesStartSuccessfully()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.eShop_AppHost>();

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync();

        // Act
        await app.StartAsync();

        // Assert - Wait for all resources to be running
        var expectedResources = new[]
        {
            "postgres",
            "redis",
            "eventbus",
            "catalogdb",
            "identitydb",
            "orderingdb",
            "webhooksdb",
            "warehousedb",
            "identity-api",
            "basket-api",
            "catalog-api",
            "ordering-api",
            "order-processor",
            "payment-processor",
            "webhooks-api",
            "warehouse-api",
            "webapp",
            "webhooksclient",
            "admin-ui"
        };

        foreach (var resourceName in expectedResources)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await app.ResourceNotifications.WaitForResourceAsync(
                resourceName,
                KnownResourceStates.Running,
                cts.Token);

            Console.WriteLine($"✓ {resourceName} is running");
        }
    }

    [Fact]
    public async Task IdentityApiHealthCheck()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.eShop_AppHost>();

        await using var app = await appHost.BuildAsync();

        // Act
        await app.StartAsync();

        // Wait for identity-api to be running
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await app.ResourceNotifications.WaitForResourceAsync(
            "identity-api",
            KnownResourceStates.Running,
            cts.Token);

        // Get HttpClient for identity-api
        var httpClient = app.CreateHttpClient("identity-api");

        // Assert - Check if identity API responds
        var response = await httpClient.GetAsync("/");
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound,
            $"Identity API health check failed with status {response.StatusCode}");
    }

    [Fact]
    public async Task WarehouseApiHealthCheck()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.eShop_AppHost>();

        await using var app = await appHost.BuildAsync();

        // Act
        await app.StartAsync();

        // Wait for dependencies first
        using var dbCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await app.ResourceNotifications.WaitForResourceAsync(
            "warehousedb",
            KnownResourceStates.Running,
            dbCts.Token);
        Console.WriteLine("warehousedb is running");

        using var catalogCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await app.ResourceNotifications.WaitForResourceAsync(
            "catalog-api",
            KnownResourceStates.Running,
            catalogCts.Token);
        Console.WriteLine("catalog-api is running");

        // Wait for warehouse-api to be running with extended timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await app.ResourceNotifications.WaitForResourceAsync(
            "warehouse-api",
            KnownResourceStates.Running,
            cts.Token);
        Console.WriteLine("warehouse-api is running");

        // Get HttpClient for warehouse-api
        var httpClient = app.CreateHttpClient("warehouse-api");

        // Assert - Check if warehouse API responds
        var response = await httpClient.GetAsync("/health");
        Console.WriteLine($"Warehouse API response: {response.StatusCode}");
        Assert.True(response.IsSuccessStatusCode,
            $"Warehouse API health check failed with status {response.StatusCode}");
    }
}
