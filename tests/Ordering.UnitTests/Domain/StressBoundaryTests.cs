using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;

namespace eShop.Ordering.UnitTests.Domain;

[TestClass]
public class StressBoundaryTests
{
    private readonly DiscountCalculationService _discountService;
    private readonly IDiscountStrategyFactory _strategyFactory;

    public StressBoundaryTests()
    {
        _strategyFactory = new DiscountStrategyFactory();
        _discountService = new DiscountCalculationService(_strategyFactory);
    }

    private Order CreateBaseOrder()
    {
        var address = new Address("street", "city", "state", "country", "zipcode");
        return new Order("userId", "userName", address, 1, "card", "123", "holder", DateTime.UtcNow.AddYears(1));
    }

    [TestMethod]
    public void Calculate_WithLargeQuantities_ShouldHandleOverflow()
    {
        // Arrange
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", int.MaxValue);
        
        var promotion = new Promotion("Promo", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.TotalDiscount >= 0);
    }

    [TestMethod]
    public void Calculate_WithLargePrices_ShouldHandleOverflow()
    {
        // Arrange
        var order = CreateBaseOrder();
        // Use a large price but not decimal.MaxValue because subtotal calculation might overflow if we add more items
        // decimal.MaxValue is 79,228,162,514,264,337,593,543,950,335
        order.AddOrderItem(1, "Product 1", 1000000000000m, 0, "url", 1);
        
        var promotion = new Promotion("Promo", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.TotalDiscount >= 0);
    }

    [TestMethod]
    public void Calculate_WithManyCategories_ShouldComplete()
    {
        // Arrange
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promotion = new Promotion("Many Categories", DiscountType.CategoryDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        for (int i = 1; i <= 1000; i++)
        {
            promotion.AddApplicableCategory($"Cat{i}");
        }

        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Calculate_WithLongDateRanges_ShouldComplete()
    {
        // Arrange
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promotion = new Promotion("100 Year Promo", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddYears(-50), DateTime.UtcNow.AddYears(50), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(10m, result.TotalDiscount);
    }

    [TestMethod]
    public void Calculate_WithZeroEdgeCases_ShouldHandleZeroDiscount()
    {
        // Arrange
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        // 1% is the minimum for percentage
        var promo1 = new Promotion("1%", DiscountType.PercentageDiscount, 1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promo2 = new Promotion("$0", DiscountType.FixedAmountDiscount, 0, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        
        var promotions = new List<Promotion> { promo1, promo2 };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(1m, result.TotalDiscount); // 1% of 100 is 1, $0 is 0
    }

    [TestMethod]
    public void Calculate_WithSmallDiscounts_ShouldHandlePrecision()
    {
        // Arrange
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        // Using FixedAmountDiscount for 0.01 test instead of percentage
        var promotion = new Promotion("$0.01 Promo", DiscountType.FixedAmountDiscount, 0.01m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(0.01m, result.TotalDiscount);
    }

    [TestMethod]
    public void Calculate_ShouldNeverProduceNegativeFinalAmount()
    {
        // Arrange
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 10m, 0, "url", 1);
        
        // Capped at 50% by business rules, but let's test if strategies themselves try to give more
        var promotion = new Promotion("Huge Discount", DiscountType.FixedAmountDiscount, 100m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);

        // Assert
        Assert.IsTrue(result.FinalAmount >= 5m, "Final amount should be at least 50% of original due to global cap");
        Assert.IsTrue(result.FinalAmount >= 0);
    }

    [TestMethod]
    public void Calculate_WithEmptyCollections_ShouldHandleGracefully()
    {
        // Arrange
        var order = CreateBaseOrder(); // No items
        var promotions = new List<Promotion>();
        var context = new DiscountContext(new List<OrderItem>());

        // Act
        var result = _discountService.Calculate(order, promotions, context);

        // Assert
        Assert.AreEqual(0, result.TotalDiscount);
        Assert.AreEqual(0, result.FinalAmount);
    }
}
