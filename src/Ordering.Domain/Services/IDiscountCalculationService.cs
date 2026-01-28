namespace eShop.Ordering.Domain.Services;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public interface IDiscountCalculationService
{
    DiscountCalculationResult Calculate(Order order, IEnumerable<Promotion> promotions, DiscountContext context);
}
