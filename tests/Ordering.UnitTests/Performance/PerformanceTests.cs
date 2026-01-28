using System.Diagnostics;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;

namespace eShop.Ordering.UnitTests.Performance;

[TestClass]
public class PerformanceTests
{
    private readonly DiscountCalculationService _discountService;
    private readonly IDiscountStrategyFactory _strategyFactory;

    public PerformanceTests()
    {
        _strategyFactory = new DiscountStrategyFactory();
        _discountService = new DiscountCalculationService(_strategyFactory);
    }

    [TestMethod]
    public void Calculate_Should_Complete_Within_100ms_For_50_Items_And_20_Promotions()
    {
        // Arrange: Create order with 50 items
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 1, "card", "123", "holder", DateTime.UtcNow.AddYears(1));
        
        for (int i = 1; i <= 50; i++)
        {
            order.AddOrderItem(i, $"Product {i}", 100m, 0, "url", 1);
        }

        // Arrange: Create 20 active promotions with different types
        var promotions = new List<Promotion>();
        for (int i = 1; i <= 20; i++)
        {
            var type = (DiscountType)((i % 5) + 1);
            promotions.Add(new Promotion(
                $"Promo {i}", 
                type, 
                Math.Max(1, i % 100), // Ensure within 1-99 for percentage
                DateTime.UtcNow.AddDays(-1), 
                DateTime.UtcNow.AddDays(1), 
                i,
                minimumQuantity: type == DiscountType.VolumeDiscount ? 1 : null));
        }
        
        var context = new DiscountContext(order.OrderItems);

        // Warm up
        _discountService.Calculate(order, promotions, context);

        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var result = _discountService.Calculate(order, promotions, context);
        
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, $"Calculation took {stopwatch.ElapsedMilliseconds}ms, which exceeds 100ms.");
    }

    [TestMethod]
    public void Calculate_Should_Complete_Within_50ms_For_10_Items_And_5_Promotions()
    {
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 1, "card", "123", "holder", DateTime.UtcNow.AddYears(1));
        
        for (int i = 1; i <= 10; i++)
        {
            order.AddOrderItem(i, $"Product {i}", 100m, 0, "url", 1);
        }

        var promotions = new List<Promotion>();
        for (int i = 1; i <= 5; i++)
        {
            promotions.Add(new Promotion(
                $"Promo {i}", 
                DiscountType.PercentageDiscount, 
                1, 
                DateTime.UtcNow.AddDays(-1), 
                DateTime.UtcNow.AddDays(1), 
                i));
        }
        
        var context = new DiscountContext(order.OrderItems);

        // Warm up
        _discountService.Calculate(order, promotions, context);

        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var result = _discountService.Calculate(order, promotions, context);
        
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50, $"Calculation took {stopwatch.ElapsedMilliseconds}ms, which exceeds 50ms.");
    }

    [TestMethod]
    public void Calculate_Should_Complete_For_100_Items_And_20_Promotions()
    {
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 1, "card", "123", "holder", DateTime.UtcNow.AddYears(1));
        
        for (int i = 1; i <= 100; i++)
        {
            order.AddOrderItem(i, $"Product {i}", 10m, 0, "url", 1);
        }

        var promotions = new List<Promotion>();
        for (int i = 1; i <= 20; i++)
        {
            promotions.Add(new Promotion(
                $"Promo {i}", 
                DiscountType.PercentageDiscount, 
                1, 
                DateTime.UtcNow.AddDays(-1), 
                DateTime.UtcNow.AddDays(1), 
                i));
        }
        
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);
        
        // Assert - just ensure it doesn't crash
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Calculate_Should_Complete_For_50_Items_And_50_Promotions()
    {
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 1, "card", "123", "holder", DateTime.UtcNow.AddYears(1));
        
        for (int i = 1; i <= 50; i++)
        {
            order.AddOrderItem(i, $"Product {i}", 100m, 0, "url", 1);
        }

        var promotions = new List<Promotion>();
        for (int i = 1; i <= 50; i++)
        {
            promotions.Add(new Promotion(
                $"Promo {i}", 
                DiscountType.PercentageDiscount, 
                1, 
                DateTime.UtcNow.AddDays(-1), 
                DateTime.UtcNow.AddDays(1), 
                i));
        }
        
        var context = new DiscountContext(order.OrderItems);

        // Act
        var result = _discountService.Calculate(order, promotions, context);
        
        // Assert - just ensure it doesn't crash
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Calculate_Should_Complete_Within_10ms_For_Single_Item_And_Single_Promotion()
    {
        // Arrange
        var address = new Address("street", "city", "state", "country", "zipcode");
        var order = new Order("userId", "userName", address, 1, "card", "123", "holder", DateTime.UtcNow.AddYears(1));
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);

        var promotion = new Promotion("Promo 1", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        
        var context = new DiscountContext(order.OrderItems);

        // Warm up
        _discountService.Calculate(order, promotions, context);

        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var result = _discountService.Calculate(order, promotions, context);
        
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10, $"Calculation took {stopwatch.ElapsedMilliseconds}ms, which exceeds 10ms.");
    }
}
