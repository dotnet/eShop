using Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands;
using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ordering.UnitTests.Application
{
    public class CompleteOrderCommandHandlerTest
    {
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<ILogger<CompleteOrderCommandHandler>> _loggerMock;

        public CompleteOrderCommandHandlerTest()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _loggerMock = new Mock<ILogger<CompleteOrderCommandHandler>>();
        }

        [Fact]
        public async Task Handle_OrderExists_ReturnsTrue()
        {
            // Arrange
            var fakeOrderId = 123;
            var fakeOrder = new Order("userId", "fakeName", "fakeAddress", "fakeCity", "fakeState", "fakeCountry", "fakeZip");
            
            // Set order to shipped state first
            fakeOrder.SetShippedStatus();

            _orderRepositoryMock
                .Setup(repo => repo.GetAsync(fakeOrderId))
                .ReturnsAsync(fakeOrder);

            _orderRepositoryMock
                .Setup(repo => repo.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var handler = new CompleteOrderCommandHandler(_orderRepositoryMock.Object, _loggerMock.Object);
            var command = new CompleteOrderCommand(fakeOrderId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Equal(OrderStatus.Completed, fakeOrder.OrderStatus);
            _orderRepositoryMock.Verify(repo => repo.GetAsync(fakeOrderId), Times.Once);
            _orderRepositoryMock.Verify(repo => repo.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_OrderNotFound_ReturnsFalse()
        {
            // Arrange
            var fakeOrderId = 123;
            _orderRepositoryMock
                .Setup(repo => repo.GetAsync(fakeOrderId))
                .ReturnsAsync((Order)null);

            var handler = new CompleteOrderCommandHandler(_orderRepositoryMock.Object, _loggerMock.Object);
            var command = new CompleteOrderCommand(fakeOrderId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result);
            _orderRepositoryMock.Verify(repo => repo.GetAsync(fakeOrderId), Times.Once);
            _orderRepositoryMock.Verify(repo => repo.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_OrderNotInShippedStatus_ThrowsDomainException()
        {
            // Arrange
            var fakeOrderId = 123;
            var fakeOrder = new Order("userId", "fakeName", "fakeAddress", "fakeCity", "fakeState", "fakeCountry", "fakeZip");
            
            // Order is in initial state (not shipped)
            _orderRepositoryMock
                .Setup(repo => repo.GetAsync(fakeOrderId))
                .ReturnsAsync(fakeOrder);

            var handler = new CompleteOrderCommandHandler(_orderRepositoryMock.Object, _loggerMock.Object);
            var command = new CompleteOrderCommand(fakeOrderId);

            // Act & Assert
            var result = await handler.Handle(command, CancellationToken.None);
            Assert.False(result);
            Assert.NotEqual(OrderStatus.Completed, fakeOrder.OrderStatus);
        }
    }
} 