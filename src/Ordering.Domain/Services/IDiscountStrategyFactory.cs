namespace eShop.Ordering.Domain.Services;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public interface IDiscountStrategyFactory
{
    IDiscountStrategy CreateStrategy(DiscountType discountType);
}
