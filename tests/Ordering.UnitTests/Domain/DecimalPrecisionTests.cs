using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;

namespace eShop.Ordering.UnitTests.Domain;

[TestClass]
public class DecimalPrecisionTests
{
    private readonly DiscountCalculationService _discountService;
    private readonly IDiscountStrategyFactory _strategyFactory;

    public DecimalPrecisionTests()
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
    public void Calculate_ShouldUseBankersRounding_Down()
    {
        // 2.125 -> 2.12 (even)
        // Order of $10, 21.25% discount -> 2.125
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 10m, 0, "url", 1);
        
        var promotion = new Promotion("21.25% Off", DiscountType.PercentageDiscount, 21.25m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        var result = _discountService.Calculate(order, promotions, context);

        Assert.AreEqual(2.12m, result.TotalDiscount);
    }

    [TestMethod]
    public void Calculate_ShouldUseBankersRounding_Up()
    {
        // 2.135 -> 2.14 (even)
        // Order of $10, 21.35% discount -> 2.135
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 10m, 0, "url", 1);
        
        var promotion = new Promotion("21.35% Off", DiscountType.PercentageDiscount, 21.35m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        var result = _discountService.Calculate(order, promotions, context);

        Assert.AreEqual(2.14m, result.TotalDiscount);
    }

    [TestMethod]
    public void Calculate_ShouldMaintainPrecisionThroughMultipleDiscounts()
    {
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 33.33m, 0, "url", 1);
        
        // 10% of 33.33 = 3.333 -> 3.33
        // Another 10% of 33.33 = 3.333 -> 3.33
        // Total = 6.66
        var promo1 = new Promotion("10% 1", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promo2 = new Promotion("10% 2", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        
        var promotions = new List<Promotion> { promo1, promo2 };
        var context = new DiscountContext(order.OrderItems);

        var result = _discountService.Calculate(order, promotions, context);

        Assert.AreEqual(6.66m, result.TotalDiscount);
        Assert.AreEqual(26.67m, result.FinalAmount);
    }

    [TestMethod]
    public void Calculate_WithCurrencyValuesHavingManyDecimalPlaces()
    {
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100.005m, 0, "url", 1); // Subtotal will be 100.00
        
        var promotion = new Promotion("10%", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        var result = _discountService.Calculate(order, promotions, context);

        // Subtotal of 100.005 rounds to 100.00 (banker's)
        // 10% of 100.00 is 10.00
        Assert.AreEqual(10.00m, result.TotalDiscount);
    }
}
