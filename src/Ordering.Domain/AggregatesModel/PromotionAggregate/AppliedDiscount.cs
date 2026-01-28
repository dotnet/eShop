using eShop.Ordering.Domain.SeedWork;

namespace eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public class AppliedDiscount : ValueObject
{
    public string PromotionId { get; private set; }
    public string PromotionName { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DateTime AppliedAt { get; private set; }
    public int ItemCount { get; private set; }

    public AppliedDiscount(string promotionId, string promotionName, decimal discountAmount, DateTime appliedAt, int itemCount)
    {
        PromotionId = promotionId;
        PromotionName = promotionName;
        DiscountAmount = discountAmount;
        AppliedAt = appliedAt;
        ItemCount = itemCount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PromotionId;
        yield return PromotionName;
        yield return DiscountAmount;
        yield return AppliedAt;
        yield return ItemCount;
    }
}
