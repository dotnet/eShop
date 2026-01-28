namespace eShop.Ordering.Domain.Services.DiscountStrategies;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public class FirstTimeCustomerDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(Promotion promotion, DiscountContext context)
    {
        // Check if this is a first-time purchase
        if (!context.IsFirstPurchase)
        {
            return 0;
        }

        // Calculate total order amount
        var orderTotal = context.Items.Sum(item => item.UnitPrice * item.Units);

        // Apply percentage discount to entire order
        var discount = orderTotal * (promotion.DiscountValue / 100m);

        // Apply banker's rounding to 2 decimal places
        discount = Math.Round(discount, 2, MidpointRounding.ToEven);

        // Apply maximum discount cap if specified
        if (promotion.MaximumDiscount.HasValue)
        {
            discount = Math.Min(discount, promotion.MaximumDiscount.Value);
        }

        return discount;
    }
}
