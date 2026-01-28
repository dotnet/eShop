using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;

namespace eShop.Ordering.UnitTests.Domain;

[TestClass]
public class FeatureSpecScenariosTests
{
    private readonly DiscountCalculationService _discountService;
    private readonly IDiscountStrategyFactory _strategyFactory;

    public FeatureSpecScenariosTests()
    {
        _strategyFactory = new DiscountStrategyFactory();
        _discountService = new DiscountCalculationService(_strategyFactory);
    }

    private Order CreateBaseOrder(string userId = "userId", bool isFirstPurchase = false)
    {
        var address = new Address("street", "city", "state", "country", "zipcode");
        // Need to check if Order has IsFirstPurchase property or if it's in the context
        return new Order(userId, "userName", address, 1, "card", "123", "holder", DateTime.UtcNow.AddYears(1));
    }

    [TestMethod]
    public void Scenario1_SinglePercentageDiscount()
    {
        // Given: Order of $100, Active promotion: 20% off
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promotion = new Promotion("20% Off", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: Discount = $20, Final = $80
        Assert.AreEqual(20m, result.TotalDiscount);
        Assert.AreEqual(80m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario2_MultipleDiscountsWithStacking()
    {
        // Given: Order of $200, Promo A: 15% off (pri 1), Promo B: 10% off (pri 2)
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 200m, 0, "url", 1);
        
        var promoA = new Promotion("15% Off", DiscountType.PercentageDiscount, 15, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promoB = new Promotion("10% Off", DiscountType.PercentageDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        var promotions = new List<Promotion> { promoA, promoB };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: Discount A = $30, Discount B = $20, Total = $50, Final = $150
        Assert.AreEqual(50m, result.TotalDiscount);
        Assert.AreEqual(150m, result.FinalAmount);
        Assert.AreEqual(30m, result.AppliedDiscounts.First(d => d.PromotionName == "15% Off").DiscountAmount);
        Assert.AreEqual(20m, result.AppliedDiscounts.First(d => d.PromotionName == "10% Off").DiscountAmount);
    }

    [TestMethod]
    public void Scenario3_DiscountCapEnforcement()
    {
        // Given: Order of $100, Promo A: 30% off (pri 1), Promo B: 25% off (pri 2)
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promoA = new Promotion("30% Off", DiscountType.PercentageDiscount, 30, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promoB = new Promotion("25% Off", DiscountType.PercentageDiscount, 25, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        var promotions = new List<Promotion> { promoA, promoB };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: Discount A = $30, Discount B = $20 (capped at 50%), Total = $50, Final = $50
        Assert.AreEqual(50m, result.TotalDiscount);
        Assert.AreEqual(50m, result.FinalAmount);
        Assert.AreEqual(30m, result.AppliedDiscounts.First(d => d.PromotionName == "30% Off").DiscountAmount);
        Assert.AreEqual(20m, result.AppliedDiscounts.First(d => d.PromotionName == "25% Off").DiscountAmount);
    }

    [TestMethod]
    public void Scenario4_MinimumOrderNotMet()
    {
        // Given: Order of $40, Promo: 20% off with $50 minimum
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 40m, 0, "url", 1);
        
        var promotion = new Promotion("20% Off", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1, minimumOrderAmount: 50m);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: No discount applied, Final = $40
        Assert.AreEqual(0, result.TotalDiscount);
        Assert.AreEqual(40m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario5_ExpiredPromotion()
    {
        // Given: Order today, Promo: 20% off (ended yesterday)
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promotion = new Promotion("Expired", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1), 1);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: No discount applied
        Assert.AreEqual(0, result.TotalDiscount);
    }

    [TestMethod]
    public void Scenario6_CategorySpecificDiscount()
    {
        // Given: Item A: Electronics $100, Item B: Clothing $50, Promo: 20% off Electronics
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Item A", 100m, 0, "url", 1); 
        order.AddOrderItem(2, "Item B", 50m, 0, "url", 1);
        
        var promotion = new Promotion("20% Electronics", DiscountType.CategoryDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        promotion.AddApplicableCategory("Electronics");
        
        var promotions = new List<Promotion> { promotion };
        
        var productCategories = new Dictionary<int, string>
        {
            { 1, "Electronics" },
            { 2, "Clothing" }
        };
        var context = new DiscountContext(order.OrderItems, isFirstPurchase: false, productCategories: productCategories);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: Discount = $20 (only on Item A), Final = $130
        Assert.AreEqual(20m, result.TotalDiscount);
        Assert.AreEqual(130m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario7_VolumeDiscount()
    {
        // Given: Order with 6 items @ $10 each ($60), Promo: Buy 5+, get 10% off
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Item A", 10m, 0, "url", 6);
        
        var promotion = new Promotion("Bulk 5+", DiscountType.VolumeDiscount, 10, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1, minimumQuantity: 5);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: Discount = $6, Final = $54
        Assert.AreEqual(6m, result.TotalDiscount);
        Assert.AreEqual(54m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario8_MaximumDiscountCap()
    {
        // Given: Order of $1000, Promo: 20% off with $100 max discount
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 1000m, 0, "url", 1);
        
        var promotion = new Promotion("20% Off Max $100", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1, maximumDiscount: 100m);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: Discount = $100 (not $200), Final = $900
        Assert.AreEqual(100m, result.TotalDiscount);
        Assert.AreEqual(900m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario9_CategoryExclusion()
    {
        // Given: Item A: Electronics $100, Item B: Sale $50, Promo: 20% off all, excluding Sale Items
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Electronics Item", 100m, 0, "url", 1);
        order.AddOrderItem(2, "Sale Item", 50m, 0, "url", 1);
        
        var promotion = new Promotion("20% Off", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        promotion.AddExcludedCategory("Sale");
        
        var promotions = new List<Promotion> { promotion };
        
        var productCategories = new Dictionary<int, string>
        {
            { 1, "Electronics" },
            { 2, "Sale" }
        };
        var context = new DiscountContext(order.OrderItems, isFirstPurchase: false, productCategories: productCategories);
        
        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: Discount = $20 (only on Item A), Final = $130
        Assert.AreEqual(20m, result.TotalDiscount);
        Assert.AreEqual(130m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario10_FirstTimeCustomer()
    {
        // Given: New customer, Order $100, Promo: 15% off first purchase
        var order = CreateBaseOrder(isFirstPurchase: true);
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promotion = new Promotion("First Timer", DiscountType.FirstTimeCustomerDiscount, 15, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promotion };
        
        // Context needs to know if it's first purchase
        var context = new DiscountContext(order.OrderItems, isFirstPurchase: true);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: Discount = $15, Final = $85
        Assert.AreEqual(15m, result.TotalDiscount);
        Assert.AreEqual(85m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario11_NoApplicableDiscounts()
    {
        // Given: Order $50, All promotions require $100 minimum
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 50m, 0, "url", 1);
        
        var promotion = new Promotion("Big Spender", DiscountType.PercentageDiscount, 20, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1, minimumOrderAmount: 100m);
        var promotions = new List<Promotion> { promotion };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then: No discount, Final = $50
        Assert.AreEqual(0, result.TotalDiscount);
        Assert.AreEqual(50m, result.FinalAmount);
    }

    [TestMethod]
    public void Scenario12_PriorityBasedApplication()
    {
        // Given: Order $100
        // - Promo A: $30 off (pri 2)
        // - Promo B: 25% off (pri 1)
        var order = CreateBaseOrder();
        order.AddOrderItem(1, "Product 1", 100m, 0, "url", 1);
        
        var promoA = new Promotion("A: $30", DiscountType.FixedAmountDiscount, 30, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 2);
        var promoB = new Promotion("B: 25%", DiscountType.PercentageDiscount, 25, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 1);
        var promotions = new List<Promotion> { promoA, promoB };
        var context = new DiscountContext(order.OrderItems);

        // When: Calculate discounts
        var result = _discountService.Calculate(order, promotions, context);

        // Then:
        // Apply B first = $25
        // Apply A next = $30
        // Check cap: $55 > $50, so cap A at $20 (actually cap A because it's applied second)
        // Final = $50
        Assert.AreEqual(50m, result.TotalDiscount);
        Assert.AreEqual(50m, result.FinalAmount);
        Assert.AreEqual(25m, result.AppliedDiscounts.First(d => d.PromotionName == "B: 25%").DiscountAmount);
        Assert.AreEqual(25m, result.AppliedDiscounts.First(d => d.PromotionName == "A: $30").DiscountAmount);
    }
}
