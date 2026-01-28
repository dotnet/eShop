using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using NSubstitute;

namespace eShop.Ordering.UnitTests.Domain;

[TestClass]
public class DiscountCalculationServiceTests
{
    private readonly IDiscountStrategyFactory _strategyFactory;
    private readonly DiscountCalculationService _calculationService;

    public DiscountCalculationServiceTests()
    {
        _strategyFactory = Substitute.For<IDiscountStrategyFactory>();
        _calculationService = new DiscountCalculationService(_strategyFactory);
    }

    [TestMethod]
    public void Calculate_ShouldThrowException_WhenOrderIsNull()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => 
            _calculationService.Calculate(null, new List<Promotion>(), new DiscountContext(new List<OrderItem>())));
    }

    [TestMethod]
    public void Calculate_ShouldReturnEmptyResult_WhenNoPromotions()
    {
        // Arrange
        var order = CreateTestOrder();
        var promotions = new List<Promotion>();
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(0, result.TotalDiscount);
        Assert.IsEmpty(result.AppliedDiscounts);
        Assert.IsEmpty(result.SkippedPromotions);
        Assert.AreEqual(order.GetTotal(), result.FinalAmount);
    }

    [TestMethod]
    public void Calculate_ShouldApplySinglePromotion()
    {
        // Arrange
        var order = CreateTestOrder(100m); // $100 order
        var promotion = new Promotion("Promo 1", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        var strategy = Substitute.For<IDiscountStrategy>();
        strategy.CalculateDiscount(promotion, context).Returns(10m);
        _strategyFactory.CreateStrategy(DiscountType.PercentageDiscount).Returns(strategy);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(10m, result.TotalDiscount);
        Assert.HasCount(1, result.AppliedDiscounts);
        Assert.AreEqual(90m, result.FinalAmount);
    }

    [TestMethod]
    public void Calculate_ShouldSortByPriority_AndApplyInOrder()
    {
        // Scenario 12: Priority-Based Application ($100 order, 25% priority 2, 10% priority 1) -> Actually lower priority number = higher priority? 
        // Plan says: "Sorts by Priority (ascending = higher priority)"
        
        // Arrange
        var order = CreateTestOrder(100m);
        var promo1 = new Promotion("High Priority", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promo2 = new Promotion("Low Priority", DiscountType.PercentageDiscount, 5, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 10);
        var promotions = new List<Promotion> { promo2, promo1 }; // Out of order
        var context = new DiscountContext(order.OrderItems);

        var strategy = Substitute.For<IDiscountStrategy>();
        // Assuming they apply in order, the order of calls to CalculateDiscount matters if we were tracking state, 
        // but here it's more about the sequence in AppliedDiscounts.
        strategy.CalculateDiscount(promo1, Arg.Any<DiscountContext>()).Returns(10m);
        strategy.CalculateDiscount(promo2, Arg.Any<DiscountContext>()).Returns(5m);
        _strategyFactory.CreateStrategy(DiscountType.PercentageDiscount).Returns(strategy);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(15m, result.TotalDiscount);
        Assert.HasCount(2, result.AppliedDiscounts);
        Assert.AreEqual("High Priority", result.AppliedDiscounts.First().PromotionName);
        Assert.AreEqual("Low Priority", result.AppliedDiscounts.Last().PromotionName);
    }

    [TestMethod]
    public void Calculate_ShouldEnforce50PercentGlobalCap()
    {
        // Scenario 3: Discount Cap Enforcement (30% + 25% capped at 50%)
        
        // Arrange
        var order = CreateTestOrder(100m);
        var promo1 = new Promotion("Promo 30%", DiscountType.PercentageDiscount, 30, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promo2 = new Promotion("Promo 25%", DiscountType.PercentageDiscount, 25, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        var promotions = new List<Promotion> { promo1, promo2 };
        var context = new DiscountContext(order.OrderItems);

        var strategy = Substitute.For<IDiscountStrategy>();
        strategy.CalculateDiscount(promo1, Arg.Any<DiscountContext>()).Returns(30m);
        strategy.CalculateDiscount(promo2, Arg.Any<DiscountContext>()).Returns(25m);
        _strategyFactory.CreateStrategy(DiscountType.PercentageDiscount).Returns(strategy);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(50m, result.TotalDiscount); // 30 + 25 = 55, capped at 100 * 0.5 = 50
        Assert.HasCount(2, result.AppliedDiscounts);
        Assert.AreEqual(30m, result.AppliedDiscounts.First().DiscountAmount);
        Assert.AreEqual(20m, result.AppliedDiscounts.Last().DiscountAmount); // Partially applied
    }

    [TestMethod]
    public void Calculate_ShouldSkipExpiredPromotions()
    {
        // Scenario 5: Expired Promotion
        
        // Arrange
        var order = CreateTestOrder(100m);
        var expiredPromo = new Promotion("Expired", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1), 1);
        var promotions = new List<Promotion> { expiredPromo };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(0, result.TotalDiscount);
        Assert.HasCount(1, result.SkippedPromotions);
        Assert.IsTrue(result.SkippedPromotions.Contains("Expired"));
    }

    [TestMethod]
    public void Calculate_ShouldSkipInactivePromotions()
    {
        // Arrange
        var order = CreateTestOrder(100m);
        var inactivePromo = new Promotion("Inactive", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        inactivePromo.Deactivate();
        var promotions = new List<Promotion> { inactivePromo };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(0, result.TotalDiscount);
        Assert.HasCount(1, result.SkippedPromotions);
    }

    [TestMethod]
    public void Calculate_ShouldSkipPromotionsWithMinimumOrderAmountNotMet()
    {
        // Scenario 4: Minimum Order Not Met ($40 order, $50 minimum)
        
        // Arrange
        var order = CreateTestOrder(40m); // $40 order
        var promotion = new Promotion("Min $50", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1, minimumOrderAmount: 50m);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(0, result.TotalDiscount);
        Assert.HasCount(1, result.SkippedPromotions);
    }

    [TestMethod]
    public void Calculate_ShouldEnforcePerPromotionMaximumDiscount()
    {
        // Arrange
        var order = CreateTestOrder(1000m);
        var promotion = new Promotion("Max $50 Discount", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1, maximumDiscount: 50m);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        var strategy = Substitute.For<IDiscountStrategy>();
        strategy.CalculateDiscount(promotion, context).Returns(100m); // 10% of 1000 is 100
        _strategyFactory.CreateStrategy(DiscountType.PercentageDiscount).Returns(strategy);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(50m, result.TotalDiscount);
        Assert.HasCount(1, result.AppliedDiscounts);
    }

    [TestMethod]
    public void Calculate_ShouldStackMultipleDiscounts()
    {
        // Scenario 2: Multiple Discounts with Stacking (15% + 10% = 25% total)
        
        // Arrange
        var order = CreateTestOrder(100m);
        var promo1 = new Promotion("15%", DiscountType.PercentageDiscount, 15, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promo2 = new Promotion("10%", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        var promotions = new List<Promotion> { promo1, promo2 };
        var context = new DiscountContext(order.OrderItems);

        var strategy = Substitute.For<IDiscountStrategy>();
        strategy.CalculateDiscount(promo1, Arg.Any<DiscountContext>()).Returns(15m);
        strategy.CalculateDiscount(promo2, Arg.Any<DiscountContext>()).Returns(10m);
        _strategyFactory.CreateStrategy(DiscountType.PercentageDiscount).Returns(strategy);

        // Act
        var result = _calculationService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(25m, result.TotalDiscount);
        Assert.HasCount(2, result.AppliedDiscounts);
    }

    private Order CreateTestOrder(decimal totalAmount = 100m)
    {
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 1, "card", "security", "holder", DateTime.UtcNow.AddYears(1));
        
        // Add one item with total price
        order.AddOrderItem(1, "Product 1", totalAmount, 0, "url", 1);
        
        return order;
    }
}
