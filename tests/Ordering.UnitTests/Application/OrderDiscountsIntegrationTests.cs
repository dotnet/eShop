using eShop.Ordering.API.Application.Commands;
using eShop.Ordering.API.Application.IntegrationEvents;
using eShop.Ordering.API.Application.Models;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eShop.Ordering.UnitTests.Application;

[TestClass]
public class OrderDiscountsIntegrationTests
{
    private readonly IOrderRepository _orderRepositoryMock;
    private readonly IPromotionRepository _promotionRepositoryMock;
    private readonly IDiscountCalculationService _discountCalculationServiceMock;
    private readonly IIdentityService _identityServiceMock;
    private readonly IMediator _mediatorMock;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventServiceMock;
    private readonly ILogger<CreateOrderCommandHandler> _loggerMock;

    public OrderDiscountsIntegrationTests()
    {
        _orderRepositoryMock = Substitute.For<IOrderRepository>();
        _promotionRepositoryMock = Substitute.For<IPromotionRepository>();
        _discountCalculationServiceMock = Substitute.For<IDiscountCalculationService>();
        _identityServiceMock = Substitute.For<IIdentityService>();
        _orderingIntegrationEventServiceMock = Substitute.For<IOrderingIntegrationEventService>();
        _mediatorMock = Substitute.For<IMediator>();
        _loggerMock = Substitute.For<ILogger<CreateOrderCommandHandler>>();
    }

    [TestMethod]
    public async Task Handle_should_retrieve_active_promotions_and_calculate_discounts()
    {
        // Arrange
        var handler = new CreateOrderCommandHandler(
            _mediatorMock, 
            _orderingIntegrationEventServiceMock, 
            _orderRepositoryMock, 
            _promotionRepositoryMock, 
            _discountCalculationServiceMock, 
            _identityServiceMock, 
            _loggerMock);

        var command = FakeOrderRequest();
        
        _promotionRepositoryMock.GetActivePromotionsAsync()
            .Returns(Task.FromResult(Enumerable.Empty<Promotion>()));
        
        // Mock the Calculate method to return an empty result
        _discountCalculationServiceMock.Calculate(Arg.Any<Order>(), Arg.Any<IEnumerable<Promotion>>(), Arg.Any<DiscountContext>())
            .Returns(new DiscountCalculationResult(10, new List<AppliedDiscount>(), new List<string>()));
            
        _orderRepositoryMock.UnitOfWork.SaveEntitiesAsync(default)
            .Returns(Task.FromResult(true));

        // Act
        await handler.Handle(command, default);

        // Assert
        // This is expected to FAIL because at this point the handler does not yet call these methods
        await _promotionRepositoryMock.Received(1).GetActivePromotionsAsync();
        _discountCalculationServiceMock.Received(1).Calculate(Arg.Any<Order>(), Arg.Any<IEnumerable<Promotion>>(), Arg.Any<DiscountContext>());
    }

    private CreateOrderCommand FakeOrderRequest()
    {
        return new CreateOrderCommand(
            new List<BasketItem> { new BasketItem { ProductId = 1, ProductName = "Test", UnitPrice = 10, Quantity = 1 } },
            userId: "1",
            userName: "testuser",
            city: "city",
            street: "street",
            state: "state",
            country: "country",
            zipcode: "zip",
            cardNumber: "1234",
            cardExpiration: DateTime.UtcNow.AddYears(1),
            cardSecurityNumber: "123",
            cardHolderName: "XXX",
            cardTypeId: 1);
    }
}
