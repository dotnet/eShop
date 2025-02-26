using Microsoft.eShopOnContainers.Services.Ordering.API.Application.DomainEventHandlers.OrderCompleted;
using Microsoft.eShopOnContainers.Services.Ordering.API.Application.IntegrationEvents;
using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
using Microsoft.eShopOnContainers.Services.Ordering.Domain.Events;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ordering.UnitTests.Application
{
    public class OrderCompletedDomainEventHandlerTest
    {
        private readonly Mock<IOrderingIntegrationEventService> _orderingIntegrationEventServiceMock;
        private readonly Mock<ILogger<OrderCompletedDomainEventHandler>> _loggerMock;

        public OrderCompletedDomainEventHandlerTest()
        {
            _orderingIntegrationEventServiceMock = new Mock<IOrderingIntegrationEventService>();
            _loggerMock = new Mock<ILogger<OrderCompletedDomainEventHandler>>();
        }

        [Fact]
        public async Task Handle_WhenOrderCompleted_PublishesIntegrationEvent()
        {
            // Arrange
            var fakeOrder = new Order("userId", "fakeName", "fakeAddress", "fakeCity", "fakeState", "fakeCountry", "fakeZip");
            fakeOrder.SetShippedStatus();
            fakeOrder.SetCompleted();

            var domainEvent = new OrderCompletedDomainEvent(fakeOrder);
            var handler = new OrderCompletedDomainEventHandler(
                _orderingIntegrationEventServiceMock.Object,
                _loggerMock.Object);

            // Act
            await handler.Handle(domainEvent, CancellationToken.None);

            // Assert
            _orderingIntegrationEventServiceMock.Verify(
                service => service.AddAndSaveEventAsync(
                    It.Is<OrderCompletedIntegrationEvent>(
                        evt => 
                            evt.OrderId == fakeOrder.Id &&
                            evt.OrderStatus == fakeOrder.OrderStatus.Name &&
                            evt.BuyerName == fakeOrder.GetBuyerName()
                    )
                ),
                Times.Once
            );
        }
    }
} 