namespace eShop.Ordering.UnitTests.Domain.Strategies;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

[TestClass]
public class FixedAmountDiscountStrategyTests : StrategyTestBase
{
    private readonly FixedAmountDiscountStrategy _strategy = new();

    [TestMethod]
    public void CalculateDiscount_SingleItem_ReturnsFixedAmount()
    {
        // Arrange
        var promotion = new Promotion("$10 Off", DiscountType.FixedAmountDiscount, 10m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        var item = CreateOrderItem(1, 100m);
        var context = CreateContext(new[] { item });

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(10m, discount);
    }

    [TestMethod]
    public void CalculateDiscount_AmountExceedsTotal_ReturnsTotalAmount()
    {
        // Arrange
        var promotion = new Promotion("$100 Off", DiscountType.FixedAmountDiscount, 100m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        var item = CreateOrderItem(1, 50m);
        var context = CreateContext(new[] { item });

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(50m, discount);
    }

    [TestMethod]
    public void CalculateDiscount_WithExcludedItems_CalculatesAgainstEligibleTotal()
    {
        // Arrange
        var promotion = new Promotion("$50 Off", DiscountType.FixedAmountDiscount, 50m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        promotion.AddExcludedCategory("Excluded");

        var items = new[]
        {
            CreateOrderItem(1, 30m), // Eligible
            CreateOrderItem(2, 100m) // Excluded
        };
        var categories = new Dictionary<int, string>
        {
            { 1, "Eligible" },
            { 2, "Excluded" }
        };
        var context = CreateContext(items, categories: categories);

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(30m, discount); // Fixed amount of $50, but only $30 of items are eligible
    }
}
