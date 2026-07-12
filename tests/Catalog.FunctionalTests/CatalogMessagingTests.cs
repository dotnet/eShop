using System.Net.Http.Json;
using System.Text.Json;

using Asp.Versioning;
using Asp.Versioning.Http;

using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.IntegrationEvents.Events;
using eShop.Catalog.API.Model;

using eShop.Testing.Common.Messaging;

using Microsoft.Extensions.DependencyInjection;

namespace eShop.Catalog.FunctionalTests;

[FlushTestLogs]
public sealed class CatalogMessagingOutboxTests(CatalogApiTestSession session)
{
    private readonly CatalogApiTestSession _session = session;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    [CatalogFunctionalTestMode(CatalogFunctionalTestMode.AspireMessagingOutbox)]
    public async Task UpdateCatalogItem_PublishesPriceChangedEventToOutboxAndSpyBus()
    {
        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(1.0));
        var host = await _session.CreateHostAsync(GetType(), nameof(UpdateCatalogItem_PublishesPriceChangedEventToOutboxAndSpyBus), handler);
        var capturingBus = host.Fixture.Services.GetRequiredService<CapturingEventBus>();
        capturingBus.Reset();

        var itemToUpdate = await GetCatalogItemAsync(host, 1);
        var originalPrice = itemToUpdate.Price;
        itemToUpdate.Price = originalPrice + 1.5m;

        var response = await host.Client.PutAsJsonAsync("/api/catalog/items", itemToUpdate, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var outboxEvents = await OutboxAssertions.GetPublishedEventsAsync<CatalogContext>(
            host.Fixture.Services,
            TestContext.Current.CancellationToken,
            nameof(ProductPriceChangedIntegrationEvent));

        Assert.Single(outboxEvents);
        Assert.Contains(capturingBus.Published, @event => @event is ProductPriceChangedIntegrationEvent priceChanged
            && priceChanged.ProductId == itemToUpdate.Id
            && priceChanged.NewPrice == itemToUpdate.Price
            && priceChanged.OldPrice == originalPrice);
    }

    private async Task<CatalogItem> GetCatalogItemAsync(CatalogApiTestHost host, int id)
    {
        var response = await host.Client.GetAsync($"/api/catalog/items/{id}", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions)!;
    }
}

[FlushTestLogs]
public sealed class CatalogMessagingRabbitMqTests(CatalogApiTestSession session)
{
    private readonly CatalogApiTestSession _session = session;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    [CatalogFunctionalTestMode(CatalogFunctionalTestMode.AspireMessagingRabbitMq)]
    public async Task UpdateCatalogItem_PublishesPriceChangedEventToRabbitMq()
    {
        IntegrationEventCapture.Reset();

        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(1.0));
        var host = await _session.CreateHostAsync(GetType(), nameof(UpdateCatalogItem_PublishesPriceChangedEventToRabbitMq), handler);

        var itemToUpdate = await GetCatalogItemAsync(host, 1);
        var originalPrice = itemToUpdate.Price;
        itemToUpdate.Price = originalPrice + 2.5m;

        var response = await host.Client.PutAsJsonAsync("/api/catalog/items", itemToUpdate, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        await IntegrationEventCapture.WaitForCountAsync(1, TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        var outboxEvents = await OutboxAssertions.GetPublishedEventsAsync<CatalogContext>(
            host.Fixture.Services,
            TestContext.Current.CancellationToken,
            nameof(ProductPriceChangedIntegrationEvent));

        Assert.Single(outboxEvents);
        Assert.Contains(IntegrationEventCapture.All, @event => @event is ProductPriceChangedIntegrationEvent priceChanged
            && priceChanged.ProductId == itemToUpdate.Id
            && priceChanged.NewPrice == itemToUpdate.Price
            && priceChanged.OldPrice == originalPrice);
    }

    private async Task<CatalogItem> GetCatalogItemAsync(CatalogApiTestHost host, int id)
    {
        var response = await host.Client.GetAsync($"/api/catalog/items/{id}", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions)!;
    }
}
