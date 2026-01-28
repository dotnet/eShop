namespace eShop.Ordering.Domain.Services;

using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public interface IDiscountStrategy
{
    decimal CalculateDiscount(Promotion promotion, DiscountContext context);
}
