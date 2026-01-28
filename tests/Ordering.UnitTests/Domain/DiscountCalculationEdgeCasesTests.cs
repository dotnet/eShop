using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

namespace Ordering.UnitTests.Domain;

[TestClass]
public class DiscountCalculationEdgeCasesTests
{
    [TestMethod]
    public void Calculate_Should_Handle_Zero_Priced_Items()
    {
        // Arrange
        var strategy = new PercentageDiscountStrategy();
        var promotion = new Promotion("10%", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        
        var item = new OrderItem(1, "Free gift", 0m, 0, "url", 1);
        var context = new DiscountContext(new List<OrderItem> { item });

        // Act
        var discount = strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(0, discount);
    }

    [TestMethod]
    public void Calculate_Should_Handle_Large_Quantities()
    {
        // Arrange
        var strategy = new PercentageDiscountStrategy();
        var promotion = new Promotion("10%", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        
        var item = new OrderItem(1, "Product", 1.0m, 0, "url", int.MaxValue);
        var context = new DiscountContext(new List<OrderItem> { item });

        // Act
        var discount = strategy.CalculateDiscount(promotion, context);

        // Assert
        var expected = Math.Round((decimal)int.MaxValue * 1.0m * 0.10m, 2, MidpointRounding.ToEven);
        Assert.AreEqual(expected, discount);
    }

    [TestMethod]
    public void Rounding_Should_Follow_Bankers_Rounding_NFR2()
    {
        // NFR2: Banker's rounding: 2.5 rounds to 2 (even), 3.5 rounds to 4 (even)
        
        var strategy = new PercentageDiscountStrategy();
        
        // 10% of 25.05 = 2.505 -> should round to 2.50 (even)
        var promo = new Promotion("10%", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var item2 = new OrderItem(2, "Item", 25.05m, 0, "url", 1);
        var context2 = new DiscountContext(new List<OrderItem> { item2 });
        var discount2 = strategy.CalculateDiscount(promo, context2);
        Assert.AreEqual(2.50m, discount2);

        // 10% of 35.15 = 3.515 -> should round to 3.52 (even)
        var item3 = new OrderItem(3, "Item", 35.15m, 0, "url", 1);
        var context3 = new DiscountContext(new List<OrderItem> { item3 });
        var discount3 = strategy.CalculateDiscount(promo, context3);
        Assert.AreEqual(3.52m, discount3);
    }

    [TestMethod]
    public void Calculate_Should_Handle_Empty_Category_Lists_For_CategoryDiscount()
    {
        // Arrange
        var strategy = new CategoryDiscountStrategy();
        var promotion = new Promotion("Empty Categories", DiscountType.CategoryDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        
        var item = new OrderItem(1, "Item", 100m, 0, "url", 1);
        var categories = new Dictionary<int, string> { { 1, "Any" } };
        var context = new DiscountContext(new List<OrderItem> { item }, productCategories: categories);

        // Act
        var discount = strategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(0, discount);
    }
}
