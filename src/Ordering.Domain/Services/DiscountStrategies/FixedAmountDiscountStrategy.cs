namespace eShop.Ordering.Domain.Services.DiscountStrategies;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public class FixedAmountDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(Promotion promotion, DiscountContext context)
    {
        // Calculate eligible total (excluding excluded categories)
        var eligibleTotal = context.Items
            .Where(item => !IsExcluded(item, promotion, context))
            .Sum(item => item.UnitPrice * item.Units);

        if (eligibleTotal == 0)
        {
            return 0;
        }

        // Fixed amount cannot exceed eligible total
        var discount = Math.Min(promotion.DiscountValue, eligibleTotal);

        // Apply maximum discount cap if specified
        if (promotion.MaximumDiscount.HasValue)
        {
            discount = Math.Min(discount, promotion.MaximumDiscount.Value);
        }

        return discount;
    }

    private bool IsExcluded(OrderItem item, Promotion promotion, DiscountContext context)
    {
        if (promotion.ExcludedCategories.Count == 0)
        {
            return false;
        }

        if (context.ProductCategories.TryGetValue(item.ProductId, out var category))
        {
            return promotion.ExcludedCategories.Contains(category);
        }

        return false;
    }
}
