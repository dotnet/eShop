using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

namespace Ordering.UnitTests.Domain;

[TestClass]
public class CategoryExclusionTests
{
    [TestMethod]
    public void CategoryDiscount_Exclusion_Should_Take_Precedence_Over_Applicable()
    {
        // Rule 5: ExcludedCategories take precedence over ApplicableCategories
        
        // Arrange
        var strategy = new CategoryDiscountStrategy();
        var promotion = new Promotion("Test Promo", DiscountType.CategoryDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        promotion.AddApplicableCategory("Electronics");
        promotion.AddExcludedCategory("Sale");

        var item1 = new OrderItem(1, "Electronic on Sale", 100m, 0, "url", 1);
        var categories = new Dictionary<int, string> { { 1, "Sale" } };
        var context = new DiscountContext(new List<OrderItem> { item1 }, productCategories: categories);

        // Act
        var discount = strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(0, discount);
    }

    [TestMethod]
    public void CategoryDiscount_Should_Exclude_Item_If_In_Excluded_Category()
    {
        // Arrange
        var strategy = new CategoryDiscountStrategy();
        var promotion = new Promotion("Test Promo", DiscountType.CategoryDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        promotion.AddApplicableCategory("Electronics");
        promotion.AddExcludedCategory("Electronics"); // Added to both

        var item = new OrderItem(1, "Laptop", 1000m, 0, "url", 1);
        var categories = new Dictionary<int, string> { { 1, "Electronics" } };
        var context = new DiscountContext(new List<OrderItem> { item }, productCategories: categories);

        // Act
        var discount = strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(0, discount); // Exclusion should win
    }

    [TestMethod]
    public void PercentageDiscount_Should_Exclude_Items_In_Excluded_Categories()
    {
        // Scenario 9: Category Exclusion (20% off all except Sale Items)
        
        // Arrange
        var strategy = new PercentageDiscountStrategy();
        var promotion = new Promotion("20% off non-sale", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        promotion.AddExcludedCategory("Sale");

        var item1 = new OrderItem(1, "Electronics", 100m, 0, "url", 1); // $100
        var item2 = new OrderItem(2, "Sale Item", 50m, 0, "url", 1);    // $50
        
        var categories = new Dictionary<int, string> { { 1, "Electronics" }, { 2, "Sale" } };
        var context = new DiscountContext(new List<OrderItem> { item1, item2 }, productCategories: categories);

        // Act
        var discount = strategy.CalculateDiscount(promotion, context);

        // Assert
        // Only item1 should be eligible: 20% of 100 = 20
        Assert.AreEqual(20m, discount);
    }
}
