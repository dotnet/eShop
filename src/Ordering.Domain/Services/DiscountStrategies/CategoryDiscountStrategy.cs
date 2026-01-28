namespace eShop.Ordering.Domain.Services.DiscountStrategies;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public class CategoryDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(Promotion promotion, DiscountContext context)
    {
        // If ApplicableCategories is specified but empty, no items qualify
        if (promotion.ApplicableCategories != null && !promotion.ApplicableCategories.Any())
        {
            return 0m;
        }

        // Filter items by applicable and excluded categories
        var eligibleTotal = context.Items
            .Where(item => IsEligible(item, promotion, context))
            .Sum(item => item.UnitPrice * item.Units);

        if (eligibleTotal == 0)
        {
            return 0;
        }

        // Calculate percentage discount on eligible items
        var discount = eligibleTotal * (promotion.DiscountValue / 100m);

        // Apply banker's rounding to 2 decimal places
        discount = Math.Round(discount, 2, MidpointRounding.ToEven);

        // Apply maximum discount cap if specified
        if (promotion.MaximumDiscount.HasValue)
        {
            discount = Math.Min(discount, promotion.MaximumDiscount.Value);
        }

        return discount;
    }

    private bool IsEligible(OrderItem item, Promotion promotion, DiscountContext context)
    {
        if (!context.ProductCategories.TryGetValue(item.ProductId, out var category))
        {
            return false;
        }

        // Exclusions take precedence
        if (promotion.ExcludedCategories.Contains(category))
        {
            return false;
        }

        // If applicable categories are specified, item must be in one of them
        if (promotion.ApplicableCategories.Count > 0)
        {
            return promotion.ApplicableCategories.Contains(category);
        }

        // If no applicable categories specified, all non-excluded items are eligible
        return true;
    }
}
