namespace eShop.Ordering.UnitTests.Domain;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services;

[TestClass]
public class DiscountStrategyTests
{
    [TestMethod]
    public void IDiscountStrategy_calculates_discount_correctly()
    {
        // Arrange
        var mockStrategy = Substitute.For<IDiscountStrategy>();
        var promotion = new Promotion("Test", DiscountType.PercentageDiscount, 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1);
        
        // Mocking an OrderContext or similar if needed, for now let's assume it takes the Order or a context
        // Based on Step 2.1: decimal CalculateDiscount(Promotion promotion, OrderContext context)
        var context = new DiscountContext(new List<OrderItem>()); 
        
        mockStrategy.CalculateDiscount(promotion, context).Returns(10m);

        // Act
        var discount = mockStrategy.CalculateDiscount(promotion, context);

        // Assert
        Assert.AreEqual(10m, discount);
    }
}
