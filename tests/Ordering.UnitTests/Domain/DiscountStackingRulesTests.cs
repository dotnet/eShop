using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using NSubstitute;

namespace Ordering.UnitTests.Domain;

[TestClass]
public class DiscountStackingRulesTests
{
    private readonly IDiscountStrategyFactory _strategyFactory;
    private readonly DiscountCalculationService _calculationService;

    public DiscountStackingRulesTests()
    {
        _strategyFactory = Substitute.For<IDiscountStrategyFactory>();
        _calculationService = new DiscountCalculationService(_strategyFactory);
    }

    [TestMethod]
    public void Calculate_Should_Not_Stack_Multiple_FirstTimeCustomerDiscounts()
    {
        // Rule 2: Multiple customer-specific discounts of the same type should not stack
        
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 0, "card", "security", "holder", DateTime.UtcNow.AddYears(1));
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promo1 = new Promotion("First Time 10%", DiscountType.FirstTimeCustomerDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promo2 = new Promotion("First Time 20%", DiscountType.FirstTimeCustomerDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        var promotions = new List<Promotion> { promo1, promo2 };
        
        var context = new DiscountContext(order.OrderItems, isFirstPurchase: true);

        var strategy = Substitute.For<IDiscountStrategy>();
        strategy.CalculateDiscount(promo1, Arg.Any<DiscountContext>()).Returns(10m);
        strategy.CalculateDiscount(promo2, Arg.Any<DiscountContext>()).Returns(20m);
        
        _strategyFactory.CreateStrategy(DiscountType.FirstTimeCustomerDiscount).Returns(strategy);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        // Only promo1 should apply because it has higher priority (1) and we don't stack same customer-specific type
        Assert.AreEqual(1, result.AppliedDiscounts.Count);
        Assert.AreEqual("First Time 10%", result.AppliedDiscounts.First().PromotionName);
        Assert.AreEqual(10m, result.TotalDiscount);
    }

    [TestMethod]
    public void Calculate_Should_Stack_FirstTimeCustomer_With_General_Percentage()
    {
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 0, "card", "security", "holder", DateTime.UtcNow.AddYears(1));
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promo1 = new Promotion("First Time 10%", DiscountType.FirstTimeCustomerDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promo2 = new Promotion("General 10%", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        var promotions = new List<Promotion> { promo1, promo2 };
        
        var context = new DiscountContext(order.OrderItems, isFirstPurchase: true);

        var strategy1 = Substitute.For<IDiscountStrategy>();
        strategy1.CalculateDiscount(promo1, Arg.Any<DiscountContext>()).Returns(10m);
        _strategyFactory.CreateStrategy(DiscountType.FirstTimeCustomerDiscount).Returns(strategy1);

        var strategy2 = Substitute.For<IDiscountStrategy>();
        strategy2.CalculateDiscount(promo2, Arg.Any<DiscountContext>()).Returns(10m);
        _strategyFactory.CreateStrategy(DiscountType.PercentageDiscount).Returns(strategy2);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        // They should stack as they are different types
        Assert.AreEqual(2, result.AppliedDiscounts.Count);
        Assert.AreEqual(20m, result.TotalDiscount);
    }
}
