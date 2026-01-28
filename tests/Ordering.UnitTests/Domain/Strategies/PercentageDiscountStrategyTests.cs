namespace eShop.Ordering.UnitTests.Domain.Strategies;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

[TestClass]
public class PercentageDiscountStrategyTests : StrategyTestBase
{
    private readonly PercentageDiscountStrategy _strategy = new();

    [TestMethod]
    public void CalculateDiscount_SingleItem_ReturnsCorrectPercentage()
    {
        // Arrange
        var promotion = new Promotion("20% Off", DiscountType.PercentageDiscount, 20m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        var item = CreateOrderItem(1, 100m);
        var context = CreateContext(new[] { item });

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(20m, discount);
    }

    [TestMethod]
    public void CalculateDiscount_MultipleItems_ReturnsPercentageOfTotal()
    {
        // Arrange
        var promotion = new Promotion("10% Off", DiscountType.PercentageDiscount, 10m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        var items = new[]
        {
            CreateOrderItem(1, 50m, 2), // $100
            CreateOrderItem(2, 50m, 1)  // $50
        };
        var context = CreateContext(items);

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(15m, discount); // 10% of $150
    }

    [TestMethod]
    public void CalculateDiscount_WithExcludedCategory_IgnoresExcludedItems()
    {
        // Arrange
        var promotion = new Promotion("10% Off", DiscountType.PercentageDiscount, 10m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        promotion.AddExcludedCategory("Sale");

        var items = new[]
        {
            CreateOrderItem(1, 100m), // Electronics
            CreateOrderItem(2, 50m)   // Sale
        };
        var categories = new Dictionary<int, string>
        {
            { 1, "Electronics" },
            { 2, "Sale" }
        };
        var context = CreateContext(items, categories: categories);

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(10m, discount); // 10% of $100, $50 item is excluded
    }

    [TestMethod]
    public void CalculateDiscount_WithMaxDiscountCap_ReturnsCappedAmount()
    {
        // Arrange
        var promotion = new Promotion("20% Off Max $10", DiscountType.PercentageDiscount, 20m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1, maximumDiscount: 10m);
        var item = CreateOrderItem(1, 100m);
        var context = CreateContext(new[] { item });

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(10m, discount); // 20% of $100 is $20, but capped at $10
    }

    [TestMethod]
    public void CalculateDiscount_EmptyOrder_ReturnsZero()
    {
        // ...
    }

    [TestMethod]
    public void CalculateDiscount_Rounding_UsesBankersRounding()
    {
        // 15% of $1.50 is 0.225. Banker's rounding to 2 places: 0.22
        var promotion = new Promotion("15% Off", DiscountType.PercentageDiscount, 15m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        var item = CreateOrderItem(1, 1.50m);
        var context = CreateContext(new[] { item });

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(0.22m, discount);

        // 15% of $2.50 is 0.375. Banker's rounding to 2 places: 0.38
        var item2 = CreateOrderItem(2, 2.50m);
        var context2 = CreateContext(new[] { item2 });
        var discount2 = _strategy.CalculateDiscount(promotion, context2);
        Assert.AreEqual(0.38m, discount2);
    }
}
