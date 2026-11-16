using eShop.IntegrationEventLogEF;
using eShop.Ordering.API.Application.IntegrationEvents.Events;
using eShop.Ordering.API.Application.IntegrationEvents.EventHandling;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace eShop.Ordering.UnitTests.Application;

[TestClass]
public class OrderApplicationServiceTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Creating_order_links_buyer_and_payment_and_enqueues_events_in_one_transaction()
    {
        using var context = new OrderApplicationTestContext();
        var order = await context.CreateOrderAsync();
        context.DbContext.ChangeTracker.Clear();
        order = await context.DbContext.Orders.Include(o => o.OrderItems).SingleAsync(TestContext.CancellationToken);
        var buyer = await context.DbContext.Buyers.Include(b => b.PaymentMethods).SingleAsync(TestContext.CancellationToken);

        Assert.AreEqual(buyer.Id, order.BuyerId);
        Assert.AreEqual(buyer.PaymentMethods.Single().Id, order.PaymentId);
        Assert.AreEqual(20m, order.GetTotal());
        Assert.IsInstanceOfType<OrderStartedIntegrationEvent>(context.Events[0]);
        var submitted = Assert.IsInstanceOfType<OrderStatusChangedToSubmittedIntegrationEvent>(context.Events[1]);
        Assert.AreEqual(order.Id, submitted.OrderId);
        var logs = await context.DbContext.Set<IntegrationEventLogEntry>().ToListAsync(TestContext.CancellationToken);
        Assert.HasCount(2, logs);
        Assert.IsTrue(logs.All(e => e.TransactionId == context.PublishedTransactions.Single()));
    }

    [TestMethod]
    public async Task Creating_second_order_reuses_buyer_and_payment_method()
    {
        using var context = new OrderApplicationTestContext();
        var command = OrderApplicationTestContext.CreateCommand();
        Assert.IsTrue(await context.Application.CreateOrderAsync(command, Guid.NewGuid(), TestContext.CancellationToken));
        Assert.IsTrue(await context.Application.CreateOrderAsync(command, Guid.NewGuid(), TestContext.CancellationToken));

        var orders = await context.DbContext.Orders.ToListAsync(TestContext.CancellationToken);
        Assert.HasCount(2, orders);
        Assert.AreEqual(orders[0].BuyerId, orders[1].BuyerId);
        Assert.AreEqual(orders[0].PaymentId, orders[1].PaymentId);
        Assert.AreEqual(1, await context.DbContext.Buyers.CountAsync(TestContext.CancellationToken));
        Assert.AreEqual(1, await context.DbContext.Payments.CountAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Duplicate_request_does_not_repeat_order_or_events()
    {
        using var context = new OrderApplicationTestContext();
        var command = OrderApplicationTestContext.CreateCommand();
        var requestId = Guid.NewGuid();
        Assert.IsTrue(await context.Application.CreateOrderAsync(command, requestId, TestContext.CancellationToken));
        Assert.IsTrue(await context.Application.CreateOrderAsync(command, requestId, TestContext.CancellationToken));

        Assert.AreEqual(1, await context.DbContext.Orders.CountAsync(TestContext.CancellationToken));
        Assert.HasCount(2, context.Events);
    }

    [TestMethod]
    public async Task Invalid_command_does_not_persist_request_order_or_events()
    {
        using var context = new OrderApplicationTestContext();
        var command = OrderApplicationTestContext.CreateCommand(DateTime.UtcNow.AddYears(-1));
        Assert.IsFalse(await context.Application.CreateOrderAsync(command, Guid.NewGuid(), TestContext.CancellationToken));

        context.DbContext.ChangeTracker.Clear();
        Assert.AreEqual(0, await context.DbContext.Orders.CountAsync(TestContext.CancellationToken));
        Assert.AreEqual(0, await context.DbContext.Set<ClientRequest>().CountAsync(TestContext.CancellationToken));
        Assert.HasCount(0, context.Events);
        Assert.HasCount(0, context.PublishedTransactions);
    }

    [TestMethod]
    public async Task Failure_after_saving_order_rolls_back_order_buyer_request_and_outbox()
    {
        using var context = new OrderApplicationTestContext();
        context.IntegrationEvents.AddAndSaveEventAsync(Arg.Any<OrderStatusChangedToSubmittedIntegrationEvent>())
            .ThrowsAsync(new InvalidOperationException("Outbox unavailable"));

        Assert.IsFalse(await context.Application.CreateOrderAsync(OrderApplicationTestContext.CreateCommand(), Guid.NewGuid(), TestContext.CancellationToken));

        context.DbContext.ChangeTracker.Clear();
        Assert.AreEqual(0, await context.DbContext.Orders.CountAsync(TestContext.CancellationToken));
        Assert.AreEqual(0, await context.DbContext.Buyers.CountAsync(TestContext.CancellationToken));
        Assert.AreEqual(0, await context.DbContext.Set<ClientRequest>().CountAsync(TestContext.CancellationToken));
        Assert.AreEqual(0, await context.DbContext.Set<IntegrationEventLogEntry>().CountAsync(TestContext.CancellationToken));
        Assert.HasCount(0, context.PublishedTransactions);
    }

    [TestMethod]
    public async Task Transient_failure_retries_the_whole_workflow_without_reusing_rolled_back_entities()
    {
        using var context = new OrderApplicationTestContext();
        var failed = false;
        context.IntegrationEvents.When(service => service.AddAndSaveEventAsync(Arg.Any<OrderStatusChangedToSubmittedIntegrationEvent>()))
            .Do(_ =>
            {
                if (!failed)
                {
                    failed = true;
                    throw new TimeoutException("Transient outbox failure");
                }
            });

        Assert.IsTrue(await context.Application.CreateOrderAsync(
            OrderApplicationTestContext.CreateCommand(), Guid.NewGuid(), TestContext.CancellationToken));

        context.DbContext.ChangeTracker.Clear();
        var order = await context.DbContext.Orders.SingleAsync(TestContext.CancellationToken);
        Assert.IsNotNull(order.BuyerId);
        Assert.IsNotNull(order.PaymentId);
        Assert.AreEqual(1, await context.DbContext.Set<ClientRequest>().CountAsync(TestContext.CancellationToken));
        Assert.AreEqual(2, await context.DbContext.Set<IntegrationEventLogEntry>().CountAsync(TestContext.CancellationToken));
        Assert.HasCount(1, context.PublishedTransactions);
    }

    [TestMethod]
    public async Task Awaiting_validation_emits_stock_items_only_on_transition()
    {
        using var context = new OrderApplicationTestContext();
        var order = await context.CreateOrderAsync();
        var command = new SetAwaitingValidationOrderStatusCommand(order.Id);
        Assert.IsTrue(await context.Application.SetAwaitingValidationOrderStatusAsync(command, TestContext.CancellationToken));
        Assert.IsTrue(await context.Application.SetAwaitingValidationOrderStatusAsync(command, TestContext.CancellationToken));

        var integrationEvent = context.Events.OfType<OrderStatusChangedToAwaitingValidationIntegrationEvent>().Single();
        Assert.AreEqual(OrderStatus.AwaitingValidation, order.OrderStatus);
        Assert.AreEqual(order.Id, integrationEvent.OrderId);
        Assert.AreEqual(2, integrationEvent.OrderStockItems.Single().Units);
    }

    [TestMethod]
    public async Task Cancel_and_ship_preserve_status_rules_and_deduplicate_requests()
    {
        using var context = new OrderApplicationTestContext();
        var order = await context.CreateOrderAsync();
        var requestId = Guid.NewGuid();
        Assert.IsTrue(await context.Application.CancelOrderAsync(new(order.Id), requestId, TestContext.CancellationToken));
        Assert.IsTrue(await context.Application.CancelOrderAsync(new(order.Id), requestId, TestContext.CancellationToken));
        Assert.AreEqual(OrderStatus.Cancelled, order.OrderStatus);
        Assert.HasCount(1, context.Events.OfType<OrderStatusChangedToCancelledIntegrationEvent>());

        Assert.IsFalse(await context.Application.ShipOrderAsync(new(order.Id), Guid.NewGuid(), TestContext.CancellationToken));
        Assert.HasCount(0, context.Events.OfType<OrderStatusChangedToShippedIntegrationEvent>());
    }

    [TestMethod]
    public async Task Paid_order_can_ship_and_emits_shipped_event()
    {
        using var context = new OrderApplicationTestContext();
        var order = await context.CreateOrderAsync();
        order.SetAwaitingValidationStatus();
        order.SetStockConfirmedStatus();
        order.SetPaidStatus();
        await context.DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var requestId = Guid.NewGuid();
        Assert.IsTrue(await context.Application.ShipOrderAsync(new(order.Id), requestId, TestContext.CancellationToken));
        Assert.IsTrue(await context.Application.ShipOrderAsync(new(order.Id), requestId, TestContext.CancellationToken));
        Assert.AreEqual(OrderStatus.Shipped, order.OrderStatus);
        Assert.HasCount(1, context.Events.OfType<OrderStatusChangedToShippedIntegrationEvent>());
    }

    [TestMethod]
    public async Task Stock_and_payment_confirmation_emit_events_only_for_actual_transitions()
    {
        using var context = new OrderApplicationTestContext();
        var order = await context.CreateOrderAsync();
        order.SetAwaitingValidationStatus();
        await context.DbContext.SaveChangesAsync(TestContext.CancellationToken);

        var stockHandler = new OrderStockConfirmedIntegrationEventHandler(
            context.Application, NullLogger<OrderStockConfirmedIntegrationEventHandler>.Instance);
        await stockHandler.Handle(new OrderStockConfirmedIntegrationEvent(order.Id));
        await stockHandler.Handle(new OrderStockConfirmedIntegrationEvent(order.Id));
        Assert.AreEqual(OrderStatus.StockConfirmed, order.OrderStatus);
        Assert.HasCount(1, context.Events.OfType<OrderStatusChangedToStockConfirmedIntegrationEvent>());

        Assert.IsTrue(await context.Application.SetPaidOrderStatusAsync(new(order.Id), TestContext.CancellationToken));
        Assert.IsTrue(await context.Application.SetPaidOrderStatusAsync(new(order.Id), TestContext.CancellationToken));
        Assert.AreEqual(OrderStatus.Paid, order.OrderStatus);
        var paid = context.Events.OfType<OrderStatusChangedToPaidIntegrationEvent>().Single();
        Assert.AreEqual(2, paid.OrderStockItems.Single().Units);
    }

    [TestMethod]
    public async Task Stock_rejection_preserves_cancellation_description_without_adding_a_new_event()
    {
        using var context = new OrderApplicationTestContext();
        var order = await context.CreateOrderAsync();
        order.SetAwaitingValidationStatus();
        await context.DbContext.SaveChangesAsync(TestContext.CancellationToken);

        Assert.IsTrue(await context.Application.SetStockRejectedOrderStatusAsync(new(order.Id, [1]), TestContext.CancellationToken));
        context.DbContext.ChangeTracker.Clear();
        order = await context.DbContext.Orders.SingleAsync(TestContext.CancellationToken);
        Assert.AreEqual(OrderStatus.Cancelled, order.OrderStatus);
        Assert.Contains("Mug", order.Description);
        Assert.HasCount(2, context.Events);
    }

    [TestMethod]
    public async Task Integration_operation_failure_propagates_and_does_not_publish()
    {
        using var context = new OrderApplicationTestContext();
        var order = await context.CreateOrderAsync();
        var previousPublications = context.PublishedTransactions.Count;
        context.IntegrationEvents.AddAndSaveEventAsync(Arg.Any<OrderStatusChangedToCancelledIntegrationEvent>())
            .ThrowsAsync(new InvalidOperationException("Outbox unavailable"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => context.Application.CancelOrderAsync(new(order.Id), cancellationToken: TestContext.CancellationToken));

        context.DbContext.ChangeTracker.Clear();
        order = await context.DbContext.Orders.SingleAsync(TestContext.CancellationToken);
        Assert.AreEqual(OrderStatus.Submitted, order.OrderStatus);
        Assert.HasCount(previousPublications, context.PublishedTransactions);
    }

    [TestMethod]
    public async Task Cancelled_operation_does_not_persist_or_publish()
    {
        using var context = new OrderApplicationTestContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => context.Application.CreateOrderAsync(OrderApplicationTestContext.CreateCommand(), Guid.NewGuid(), cancellation.Token));
        Assert.AreEqual(0, await context.DbContext.Orders.CountAsync(TestContext.CancellationToken));
        Assert.HasCount(0, context.PublishedTransactions);
    }

    [TestMethod]
    public async Task Draft_calculation_does_not_write_or_publish()
    {
        using var context = new OrderApplicationTestContext();
        var draft = await context.Application.CreateOrderDraftAsync(new("buyer-id",
            [new BasketItem { ProductId = 1, ProductName = "Mug", UnitPrice = 10, Quantity = 2 }]), TestContext.CancellationToken);

        Assert.AreEqual(20m, draft.Total);
        Assert.AreEqual(0, await context.DbContext.Orders.CountAsync(TestContext.CancellationToken));
        Assert.HasCount(0, context.Events);
        Assert.HasCount(0, context.PublishedTransactions);
    }
}
