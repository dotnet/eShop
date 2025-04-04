using eShop.Ordering.API.Application.Commands;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

namespace eShop.Ordering.UnitTests.Application;

public class CompleteOrderByShipmentCommandHandlerTest
{
    private readonly IOrderRepository _orderRepositoryMock;
    private readonly IMediator _mediator;
    private readonly ILogger<CompleteOrderByShipmentCommandHandler> _loggerMock;

    public CompleteOrderByShipmentCommandHandlerTest()
    {
        _orderRepositoryMock = Substitute.For<IOrderRepository>();
        _mediator = Substitute.For<IMediator>();
        _loggerMock = Substitute.For<ILogger<CompleteOrderByShipmentCommandHandler>>();
    }

    [TestMethod]
    public async Task Handle_ReturnsFalse_IfOrderNotFound()
    {
        // Arrange
        var command = new CompleteOrderByShipmentCommand(1, "123", "Carrier", DateTime.UtcNow);
        _orderRepositoryMock.GetAsync(Arg.Any<int>()).Returns(Task.FromResult<Order>(null));

        var handler = new CompleteOrderByShipmentCommandHandler(_orderRepositoryMock);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Handle_ReturnsFalse_IfOrderNotShipped()
    {
        // Arrange
        var order = FakeOrder();
        order.SetAwaitingValidationStatus(); // Set status to something other than Shipped
        var command = new CompleteOrderByShipmentCommand(1, "123", "Carrier", DateTime.UtcNow);
        _orderRepositoryMock.GetAsync(Arg.Any<int>()).Returns(Task.FromResult(order));

        var handler = new CompleteOrderByShipmentCommandHandler(_orderRepositoryMock);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Handle_UpdatesOrderAndSaves_IfOrderShipped()
    {
        // Arrange
        var order = FakeOrder();
        order.SetShippedStatus(); // Set status to Shipped
        var command = new CompleteOrderByShipmentCommand(1, "123", "Carrier", DateTime.UtcNow);
        _orderRepositoryMock.GetAsync(Arg.Any<int>()).Returns(Task.FromResult(order));
        _orderRepositoryMock.UnitOfWork.SaveEntitiesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var handler = new CompleteOrderByShipmentCommandHandler(_orderRepositoryMock);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await _orderRepositoryMock.UnitOfWork.Received(1).SaveEntitiesAsync(Arg.Any<CancellationToken>());
    }

    private Order FakeOrder()
    {
        return new Order("1", "fakeName", new Address("street", "city", "state", "country", "zipcode"), 1, "12", "111", "fakeName", DateTime.UtcNow.AddYears(1));
    }
}
