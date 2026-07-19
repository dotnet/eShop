namespace eShop.Coupon.API.Apis.Dtos;

using eShop.Coupon.Domain.Entities;

public record CouponDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public CouponType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public int? MaxUsageCount { get; init; }
    public int CurrentUsageCount { get; init; }
    public bool IsActive { get; init; }
}

public record CreateCouponRequest
{
    public string Code { get; init; } = string.Empty;
    public CouponType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public int? MaxUsageCount { get; init; }
}

public record UpdateCouponRequest
{
    public bool? IsActive { get; init; }
    public int? MaxUsageCount { get; init; }
}
