using System.Reflection;
using System.Text;
using System.Text.Json;

using eShop.Ordering.API.Application.Commands;
using eShop.Ordering.API.Application.IntegrationEvents.Events;
using eShop.Ordering.API.Application.Models;
using eShop.Ordering.Infrastructure;

using eShop.Testing.Common.Messaging;

using Microsoft.Extensions.DependencyInjection;

namespace eShop.Ordering.FunctionalTests;

[FlushTestLogs]
public sealed class OrderingMessagingOutboxTests(OrderingApiTestSession session)
{
    private readonly OrderingApiTestSession _session = session;

    [Fact]
    [OrderingFunctionalTestMode(OrderingFunctionalTestMode.AspireMessagingOutbox)]
    public async Task AddNewOrder_PublishesIntegrationEventsToOutboxAndSpyBus()
    {
        var host = await _session.CreateHostAsync(GetType(), nameof(AddNewOrder_PublishesIntegrationEventsToOutboxAndSpyBus));
        var capturingBus = host.Fixture.Services.GetRequiredService<CapturingEventBus>();
        capturingBus.Reset();

        var response = await host.Client.PostAsync(
            "api/orders",
            CreateOrderContent(),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var outboxEvents = await OutboxAssertions.GetPublishedEventsAsync<OrderingContext>(
            host.Fixture.Services,
            TestContext.Current.CancellationToken,
            nameof(OrderStartedIntegrationEvent),
            nameof(OrderStatusChangedToSubmittedIntegrationEvent));

        Assert.Equal(2, outboxEvents.Count);
        Assert.Contains(outboxEvents, entry => entry.EventTypeName.EndsWith(nameof(OrderStartedIntegrationEvent)));
        Assert.Contains(outboxEvents, entry => entry.EventTypeName.EndsWith(nameof(OrderStatusChangedToSubmittedIntegrationEvent)));
        Assert.Contains(capturingBus.Published, @event => @event is OrderStartedIntegrationEvent started && started.UserId == "1");
        Assert.Contains(capturingBus.Published, @event => @event is OrderStatusChangedToSubmittedIntegrationEvent submitted && submitted.BuyerIdentityGuid == "1");
    }

    private static StringContent CreateOrderContent()
    {
        var item = new BasketItem
        {
            Id = "1",
            ProductId = 12,
            ProductName = "Test",
            UnitPrice = 10,
            OldUnitPrice = 9,
            Quantity = 1,
            PictureUrl = null
        };

        var request = new CreateOrderRequest(
            "1",
            "TestUser",
            "Redmond",
            "555 Cherry St",
            "WA",
            "USA",
            "98052",
            "XXXXXXXXXXXX0005",
            "Test User",
            DateTime.UtcNow.AddYears(1),
            "123",
            1,
            "test buyer",
            [item]);

        return new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        {
            Headers = { { "x-requestid", Guid.NewGuid().ToString() } }
        };
    }
}

[FlushTestLogs]
public sealed class OrderingMessagingRabbitMqTests(OrderingApiTestSession session)
{
    private readonly OrderingApiTestSession _session = session;

    [Fact]
    [OrderingFunctionalTestMode(OrderingFunctionalTestMode.AspireMessagingRabbitMq)]
    public async Task AddNewOrder_PublishesIntegrationEventsToRabbitMq()
    {
        IntegrationEventCapture.Reset();

        var host = await _session.CreateHostAsync(GetType(), nameof(AddNewOrder_PublishesIntegrationEventsToRabbitMq));

        var response = await host.Client.PostAsync(
            "api/orders",
            CreateOrderContent(),
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        await IntegrationEventCapture.WaitForCountAsync(2, TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        var outboxEvents = await OutboxAssertions.GetPublishedEventsAsync<OrderingContext>(
            host.Fixture.Services,
            TestContext.Current.CancellationToken,
            nameof(OrderStartedIntegrationEvent),
            nameof(OrderStatusChangedToSubmittedIntegrationEvent));

        Assert.Equal(2, outboxEvents.Count);
        Assert.Contains(IntegrationEventCapture.All, @event => @event is OrderStartedIntegrationEvent started && started.UserId == "1");
        Assert.Contains(IntegrationEventCapture.All, @event => @event is OrderStatusChangedToSubmittedIntegrationEvent submitted && submitted.BuyerIdentityGuid == "1");
    }

    private static StringContent CreateOrderContent()
    {
        var item = new BasketItem
        {
            Id = "1",
            ProductId = 12,
            ProductName = "Test",
            UnitPrice = 10,
            OldUnitPrice = 9,
            Quantity = 1,
            PictureUrl = null
        };

        var request = new CreateOrderRequest(
            "1",
            "TestUser",
            "Redmond",
            "555 Cherry St",
            "WA",
            "USA",
            "98052",
            "XXXXXXXXXXXX0005",
            "Test User",
            DateTime.UtcNow.AddYears(1),
            "123",
            1,
            "test buyer",
            [item]);

        return new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        {
            Headers = { { "x-requestid", Guid.NewGuid().ToString() } }
        };
    }
}
