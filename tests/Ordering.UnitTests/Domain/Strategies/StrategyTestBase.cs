namespace eShop.Ordering.UnitTests.Domain.Strategies;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.Services;

public abstract class StrategyTestBase
{
    protected OrderItem CreateOrderItem(int productId, decimal unitPrice, int units = 1)
    {
        return new OrderItem(productId, $"Product {productId}", unitPrice, 0, "http://test.com/img.png", units);
    }

    protected DiscountContext CreateContext(IEnumerable<OrderItem> items, bool isFirstPurchase = false, IDictionary<int, string> categories = null)
    {
        return new DiscountContext(items, isFirstPurchase, categories);
    }
}
