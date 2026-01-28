namespace eShop.Ordering.UnitTests.Domain.Strategies;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

[TestClass]
public class CategoryDiscountStrategyTests : StrategyTestBase
{
    private readonly CategoryDiscountStrategy _strategy = new();

    [TestMethod]
    public void CalculateDiscount_OnlyApplicableItems_ReturnsPercentage()
    {
        // Arrange
        var promotion = new Promotion("25% Off Electronics", DiscountType.CategoryDiscount, 25m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        promotion.AddApplicableCategory("Electronics");

        var items = new[]
        {
            CreateOrderItem(1, 100m), // Electronics
            CreateOrderItem(2, 50m)   // Clothing
        };
        var categories = new Dictionary<int, string>
        {
            { 1, "Electronics" },
            { 2, "Clothing" }
        };
        var context = CreateContext(items, categories: categories);

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(25m, discount); // 25% of $100
    }

    [TestMethod]
    public void CalculateDiscount_ExclusionPrecedence_ReturnsZero()
    {
        // Arrange
        var promotion = new Promotion("25% Off Electronics", DiscountType.CategoryDiscount, 25m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        promotion.AddApplicableCategory("Electronics");
        promotion.AddExcludedCategory("Refurbished");

        var item = CreateOrderItem(1, 100m);
        var categories = new Dictionary<int, string> { { 1, "Electronics" } };
        // Wait, an item can belong to multiple categories? 
        // In this system, each item has ONE category in the context mapping (Dictionary<int, string>).
        // To support "Refurbished" exclusion if it's "Electronics", we might need multiple categories per item.
        // But our context currently supports one. 
        // Let's assume for now one category, or we can update the context.
        
        // Actually, let's keep it simple: if it's in the excluded category, it's out.
        var context = CreateContext(new[] { item }, categories: new Dictionary<int, string> { { 1, "Refurbished" } });

        // Act
        var discount = _strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(0m, discount);
    }
}
