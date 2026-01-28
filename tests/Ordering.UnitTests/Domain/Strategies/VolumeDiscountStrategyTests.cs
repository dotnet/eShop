namespace eShop.Ordering.UnitTests.Domain.Strategies;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

[TestClass]
public class VolumeDiscountStrategyTests : StrategyTestBase
{
    private readonly VolumeDiscountStrategy _strategy = new();

    [TestMethod]
    public void CalculateDiscount_ThresholdMet_ReturnsPercentage()
    {
        // Arrange
        // Buy 5+, get 10% off
        var promotion = new Promotion("Volume 5+", DiscountType.VolumeDiscount, 10m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1, minimumQuantity: 5);
        var item = CreateOrderItem(1, 10m, 6); // 6 units
        var context = CreateContext(new[] { item });

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(6m, discount); // 10% of $60
    }

    [TestMethod]
    public void CalculateDiscount_ThresholdNotMet_ReturnsZero()
    {
        // Arrange
        var promotion = new Promotion("Volume 5+", DiscountType.VolumeDiscount, 10m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1, minimumQuantity: 5);
        var item = CreateOrderItem(1, 10m, 4); // 4 units
        var context = CreateContext(new[] { item });

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(0m, discount);
    }

    [TestMethod]
    public void CalculateDiscount_MultipleItemsThresholdMet_ReturnsPercentage()
    {
        // Arrange
        var promotion = new Promotion("Volume 5+", DiscountType.VolumeDiscount, 10m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1, minimumQuantity: 5);
        var items = new[]
        {
            CreateOrderItem(1, 10m, 3), // 3 units
            CreateOrderItem(2, 10m, 2)  // 2 units
        }; // Total 5 units
        var context = CreateContext(items);

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(5m, discount); // 10% of $50
    }
}
