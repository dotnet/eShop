using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;
using eShop.Ordering.Domain.Services.DiscountStrategies;

namespace Ordering.UnitTests.Domain;

[TestClass]
public class ComplexDiscountScenariosTests
{
    private readonly IDiscountStrategyFactory _strategyFactory;
    private readonly IDiscountCalculationService _calculationService;

    public ComplexDiscountScenariosTests()
    {
        _strategyFactory = new DiscountStrategyFactory();
        _calculationService = new DiscountCalculationService(_strategyFactory);
    }

    [TestMethod]
    public void Scenario_9_Category_Exclusion_Complex()
    {
        // Scenario 9: Electronics $100 + Sale Items $50, 20% off excluding Sale
        
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 0, "card", "security", "holder", DateTime.UtcNow.AddYears(1));
        order.AddOrderItem(1, "Laptop", 100m, 0, "url", 1);
        order.AddOrderItem(2, "Cheap Phone (Sale)", 50m, 0, "url", 1);
        
        var promotion = new Promotion("20% off non-sale", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        promotion.AddExcludedCategory("Sale");

        var categories = new Dictionary<int, string> { { 1, "Electronics" }, { 2, "Sale" } };
        var context = new DiscountContext(order.OrderItems, productCategories: categories);

        // Act
        var result = _calculationService.Calculate(order, new[] { promotion }, context);

        // Assert
        // Subtotal = 150. Item 2 excluded. 20% of 100 = 20.
        Assert.AreEqual(20m, result.TotalDiscount);
        Assert.AreEqual(130m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario_11_No_Applicable_Discounts()
    {
        // Scenario 11: $50 order, all promotions require $100 minimum
        
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 0, "card", "security", "holder", DateTime.UtcNow.AddYears(1));
        order.AddOrderItem(1, "Cheap Item", 50m, 0, "url", 1);
        
        var promo1 = new Promotion("Min $100 A", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1, minimumOrderAmount: 100m);
        var promo2 = new Promotion("Min $100 B", DiscountType.FixedAmountDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2, minimumOrderAmount: 100m);

        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _calculationService.Calculate(order, new[] { promo1, promo2 }, context);

        // Assert
        Assert.AreEqual(0, result.TotalDiscount);
        Assert.AreEqual(50m, result.FinalAmount);
        Assert.AreEqual(2, result.SkippedPromotions.Count);
    }

    [TestMethod]
    public void Mix_Of_Percentage_Fixed_And_Category_Discounts()
    {
        // Order: 
        // 1x Laptop (Electronics) $1000
        // 2x Mouse (Accessories) $25 each ($50 total)
        // Subtotal: $1050
        
        // Promos:
        // 1. Priority 1: Category Discount 10% on Electronics (Max $50)
        // 2. Priority 2: Fixed Amount Discount $20 (on everything)
        // 3. Priority 3: Percentage Discount 5% (on everything, except Accessories)
        
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 0, "card", "security", "holder", DateTime.UtcNow.AddYears(1));
        order.AddOrderItem(1, "Laptop", 1000m, 0, "url", 1);
        order.AddOrderItem(2, "Mouse", 25m, 0, "url", 2);
        
        var promo1 = new Promotion("10% Electronics (Max $50)", DiscountType.CategoryDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1, maximumDiscount: 50m);
        promo1.AddApplicableCategory("Electronics");
        
        var promo2 = new Promotion("$20 Off", DiscountType.FixedAmountDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        
        var promo3 = new Promotion("5% non-accessories", DiscountType.PercentageDiscount, 5, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 3);
        promo3.AddExcludedCategory("Accessories");

        var categories = new Dictionary<int, string> { { 1, "Electronics" }, { 2, "Accessories" } };
        var context = new DiscountContext(order.OrderItems, productCategories: categories);

        // Act
        var result = _calculationService.Calculate(order, new[] { promo1, promo2, promo3 }, context);

        // Assertions:
        // Promo 1: 10% of 1000 is 100, but capped at 50. -> $50
        // Promo 2: Fixed $20. -> $20
        // Promo 3: 5% of (Subtotal - Accessories) = 5% of 1000 = 50. -> $50
        // Total expected discount: 50 + 20 + 50 = $120
        
        Assert.AreEqual(3, result.AppliedDiscounts.Count);
        Assert.AreEqual(120m, result.TotalDiscount);
        Assert.AreEqual(1050m - 120m, result.FinalAmount);
    }
}
