using System.Collections.ObjectModel;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

namespace eShop.Ordering.Domain.Services;

public class DiscountContext
{
    public IEnumerable<OrderItem> Items { get; }
    public bool IsFirstPurchase { get; }
    public IReadOnlyDictionary<int, string> ProductCategories { get; }

    public DiscountContext(IEnumerable<OrderItem> items, bool isFirstPurchase = false, IDictionary<int, string> productCategories = null)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        IsFirstPurchase = isFirstPurchase;
        ProductCategories = new ReadOnlyDictionary<int, string>(productCategories ?? new Dictionary<int, string>());
    }
}
