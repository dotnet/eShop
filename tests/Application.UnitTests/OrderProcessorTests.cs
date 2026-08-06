using eShop.EventBus.Abstractions;
using eShop.OrderProcessor;
using eShop.OrderProcessor.Events;
using eShop.OrderProcessor.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace eShop.Application.UnitTests;

[TestClass]
public class OrderProcessorTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task PublishesGracePeriodConfirmationForEveryEligibleOrder()
    {
        var repository = Substitute.For<IGracePeriodOrdersRepository>();
        repository.GetConfirmedGracePeriodOrdersAsync(
                TimeSpan.FromMinutes(1),
                Arg.Any<CancellationToken>())
            .Returns([12, 34]);
        var eventBus = Substitute.For<IEventBus>();
        var service = new GracePeriodManagerService(
            Options.Create(new BackgroundTaskOptions
            {
                GracePeriodTime = 1,
                CheckUpdateTime = 30
            }),
            eventBus,
            NullLogger<GracePeriodManagerService>.Instance,
            repository);

        await service.CheckConfirmedGracePeriodOrders(TestContext.CancellationToken);

        await eventBus.Received(1).PublishAsync(
            Arg.Is<GracePeriodConfirmedIntegrationEvent>(e => e.OrderId == 12));
        await eventBus.Received(1).PublishAsync(
            Arg.Is<GracePeriodConfirmedIntegrationEvent>(e => e.OrderId == 34));
    }
}
