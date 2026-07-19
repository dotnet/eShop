namespace eShop.Coupon.API.Persistence;

using eShop.Coupon.Domain.Entities;
using eShop.Coupon.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;

public class CouponContextSeed : IDbSeeder<CouponContext>
{
    public async Task SeedAsync(CouponContext context)
    {
        if (await context.Coupons.AnyAsync())
        {
            return; // Already seeded
        }

        var coupons = new List<Coupon>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "WELCOME10",
                DiscountType = CouponType.Fixed,
                DiscountValue = 10m,
                MinimumOrderAmount = 50m,
                ExpiryDate = DateTime.UtcNow.AddDays(90),
                MaxUsageCount = 1000,
                CurrentUsageCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "SAVE15",
                DiscountType = CouponType.Percentage,
                DiscountValue = 15m,
                MinimumOrderAmount = 100m,
                ExpiryDate = DateTime.UtcNow.AddDays(60),
                MaxUsageCount = 500,
                CurrentUsageCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Coupons.AddRangeAsync(coupons);
        await context.SaveChangesAsync();
    }
}
