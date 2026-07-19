namespace eShop.Coupon.Domain.Entities;

public class Coupon
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? MaxUsageCount { get; set; }
    public int CurrentUsageCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsValid()
    {
        if (!IsActive)
            return false;

        if (ExpiryDate.HasValue && ExpiryDate < DateTime.UtcNow)
            return false;

        if (MaxUsageCount.HasValue && CurrentUsageCount >= MaxUsageCount)
            return false;

        return true;
    }

    public bool CanBeApplied(decimal orderTotal)
    {
        if (!IsValid())
            return false;

        if (MinimumOrderAmount.HasValue && orderTotal < MinimumOrderAmount)
            return false;

        return true;
    }

    public decimal CalculateDiscount(decimal itemPrice, decimal itemQuantity)
    {
        var itemTotal = itemPrice * itemQuantity;

        return DiscountType switch
        {
            CouponType.Fixed => Math.Min(DiscountValue, itemTotal),
            CouponType.Percentage => (itemTotal * DiscountValue) / 100,
            _ => 0
        };
    }

    public void IncrementUsage()
    {
        CurrentUsageCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum CouponType
{
    Fixed = 0,
    Percentage = 1
}
