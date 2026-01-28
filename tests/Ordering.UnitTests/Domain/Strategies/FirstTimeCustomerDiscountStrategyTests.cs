namespace eShop.Ordering.UnitTests.Domain.Strategies;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

[TestClass]
public class FirstTimeCustomerDiscountStrategyTests : StrategyTestBase
{
    private readonly FirstTimeCustomerDiscountStrategy _strategy = new();

    [TestMethod]
    public void CalculateDiscount_IsFirstPurchase_ReturnsPercentage()
    {
        // Arrange
        var promotion = new Promotion("First Time 15%", DiscountType.FirstTimeCustomerDiscount, 15m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        var item = CreateOrderItem(1, 100m);
        var context = CreateContext(new[] { item }, isFirstPurchase: true);

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(15m, discount);
    }

    [TestMethod]
    public void CalculateDiscount_IsNotFirstPurchase_ReturnsZero()
    {
        // Arrange
        var promotion = new Promotion("First Time 15%", DiscountType.FirstTimeCustomerDiscount, 15m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        var item = CreateOrderItem(1, 100m);
        var context = CreateContext(new[] { item }, isFirstPurchase: false);

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(0m, discount);
    }
}
