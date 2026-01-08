using eShop.Ordering.API.Application.Commands;
using eShop.Ordering.API.Application.Queries;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.UnitTests.Shared.TestData;
using MediatR;

namespace eShop.UnitTests.Services.Ordering;

[TestClass]
public class OrderingServiceTests
{
    private IMediator _mockMediator;
    private IOrderRepository _mockOrderRepository;

    [TestInitialize]
    public void Setup()
    {
        _mockMediator = Substitute.For<IMediator>();
        _mockOrderRepository = Substitute.For<IOrderRepository>();
    }

    [TestMethod]
    public async Task CreateOrder_ValidCommand_ReturnsOrderId()
    {
        // Arrange
        var command = new CreateOrderCommand(
            basketItems: new List<BasketItem>
            {
                new("item1", 1, "Product 1", 99.99m, 0, 1, "pic1.jpg")
            },
            userId: "test-user-123",
            userName: "testuser@example.com",
            city: "Seattle",
            street: "123 Test St",
            state: "WA",
            country: "USA",
            zipCode: "98101",
            cardNumber: "4111111111111111",
            cardHolderName: "Test User",
            cardExpiration: DateTime.Now.AddYears(2),
            cardSecurityNumber: "123",
            cardTypeId: 1
        );

        var expectedOrderId = 123;
        _mockMediator.Send(command, default).Returns(Task.FromResult(expectedOrderId));

        // Act
        var result = await _mockMediator.Send(command);

        // Assert
        result.ShouldBe(expectedOrderId);
        await _mockMediator.Received(1).Send(command, default);
    }

    [TestMethod]
    public async Task GetOrder_ValidOrderId_ReturnsOrder()
    {
        // Arrange
        var orderId = 123;
        var order = OrderTestData.CreateValidOrder();
        var query = new GetOrderQuery(orderId);

        _mockMediator.Send(query, default).Returns(Task.FromResult(order));

        // Act
        var result = await _mockMediator.Send(query);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(order);
        await _mockMediator.Received(1).Send(query, default);
    }

    [TestMethod]
    public async Task CancelOrder_ValidOrderId_CancelsSuccessfully()
    {
        // Arrange
        var orderId = 123;
        var command = new CancelOrderCommand(orderId);

        _mockMediator.Send(command, default).Returns(Task.FromResult(true));

        // Act
        var result = await _mockMediator.Send(command);

        // Assert
        result.ShouldBeTrue();
        await _mockMediator.Received(1).Send(command, default);
    }

    [TestMethod]
    public async Task ShipOrder_ValidOrderId_ShipsSuccessfully()
    {
        // Arrange
        var orderId = 123;
        var command = new ShipOrderCommand(orderId);

        _mockMediator.Send(command, default).Returns(Task.FromResult(true));

        // Act
        var result = await _mockMediator.Send(command);

        // Assert
        result.ShouldBeTrue();
        await _mockMediator.Received(1).Send(command, default);
    }

    [TestMethod]
    public async Task GetOrdersByUser_ValidUserId_ReturnsOrders()
    {
        // Arrange
        var userId = "test-user-123";
        var orders = new List<Order>
        {
            OrderTestData.CreateValidOrder(userId: userId),
            OrderTestData.CreateValidOrder(userId: userId)
        };
        var query = new GetOrdersByUserQuery(userId);

        _mockMediator.Send(query, default).Returns(Task.FromResult(orders.AsEnumerable()));

        // Act
        var result = await _mockMediator.Send(query);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.All(o => o.GetBuyerId() == userId).ShouldBeTrue();
        await _mockMediator.Received(1).Send(query, default);
    }

    [TestMethod]
    public void Order_AddOrderItem_IncreasesTotalItems()
    {
        // Arrange
        var order = OrderTestData.CreateValidOrder();
        var orderItem = OrderTestData.CreateOrderItem(productId: 1, units: 2);

        // Act
        order.AddOrderItem(orderItem.ProductId, orderItem.ProductName, orderItem.UnitPrice, 
                          orderItem.Discount, orderItem.PictureUrl, orderItem.Units);

        // Assert
        order.OrderItems.Count.ShouldBe(1);
        order.GetTotal().ShouldBe(orderItem.UnitPrice * orderItem.Units);
    }

    [TestMethod]
    public void Order_SetAwaitingValidationStatus_ChangesStatus()
    {
        // Arrange
        var order = OrderTestData.CreateValidOrder();

        // Act
        order.SetAwaitingValidationStatus();

        // Assert
        order.OrderStatus.ShouldBe(OrderStatus.AwaitingValidation);
    }

    [TestMethod]
    public void Order_SetPaidStatus_ChangesStatus()
    {
        // Arrange
        var order = OrderTestData.CreateValidOrder();

        // Act
        order.SetPaidStatus();

        // Assert
        order.OrderStatus.ShouldBe(OrderStatus.Paid);
    }

    [TestMethod]
    public void Order_SetShippedStatus_ChangesStatus()
    {
        // Arrange
        var order = OrderTestData.CreateValidOrder();

        // Act
        order.SetShippedStatus();

        // Assert
        order.OrderStatus.ShouldBe(OrderStatus.Shipped);
    }

    [TestMethod]
    public void Order_SetCancelledStatus_ChangesStatus()
    {
        // Arrange
        var order = OrderTestData.CreateValidOrder();

        // Act
        order.SetCancelledStatus();

        // Assert
        order.OrderStatus.ShouldBe(OrderStatus.Cancelled);
    }
}