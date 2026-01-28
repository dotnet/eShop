namespace eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

/// <summary>
/// Represents the result of a discount calculation.
/// </summary>
public class DiscountCalculationResult
{
    public decimal OriginalAmount { get; private set; }
    public IReadOnlyCollection<AppliedDiscount> AppliedDiscounts { get; private set; }
    public IReadOnlyCollection<string> SkippedPromotions { get; private set; }

    /// <summary>
    /// Total discount amount calculated from all applied discounts.
    /// </summary>
    public decimal TotalDiscount => AppliedDiscounts.Sum(d => d.DiscountAmount);
    
    /// <summary>
    /// Final amount after applying all discounts.
    /// </summary>
    public decimal FinalAmount => OriginalAmount - TotalDiscount;

    public DiscountCalculationResult(decimal originalAmount, IEnumerable<AppliedDiscount> appliedDiscounts, IEnumerable<string> skippedPromotions)
    {
        OriginalAmount = originalAmount;
        AppliedDiscounts = appliedDiscounts.ToList().AsReadOnly();
        SkippedPromotions = skippedPromotions.ToList().AsReadOnly();
    }
}
