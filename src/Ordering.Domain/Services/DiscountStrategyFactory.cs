namespace eShop.Ordering.Domain.Services;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Services.DiscountStrategies;

public class DiscountStrategyFactory : IDiscountStrategyFactory
{
    public IDiscountStrategy CreateStrategy(DiscountType discountType)
    {
        return discountType switch
        {
            DiscountType.PercentageDiscount => new PercentageDiscountStrategy(),
            DiscountType.FixedAmountDiscount => new FixedAmountDiscountStrategy(),
            DiscountType.CategoryDiscount => new CategoryDiscountStrategy(),
            DiscountType.VolumeDiscount => new VolumeDiscountStrategy(),
            DiscountType.FirstTimeCustomerDiscount => new FirstTimeCustomerDiscountStrategy(),
            _ => throw new ArgumentException($"Unknown discount type: {discountType}", nameof(discountType))
        };
    }
}
