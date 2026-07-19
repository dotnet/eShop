namespace eShop.Coupon.Infrastructure.Persistence;

using eShop.Coupon.Application.Interfaces;
using eShop.Coupon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class CouponDataAccess : ICouponDataAccess
{
    private readonly CouponContext _context;

    public CouponDataAccess(CouponContext context)
    {
        _context = context;
    }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task CreateAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        _context.Coupons.Attach(coupon);
        _context.Entry(coupon).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var coupon = await GetByIdAsync(id, cancellationToken);
        if (coupon != null)
        {
            coupon.IsActive = false;
            coupon.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(coupon, cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Coupons
            .AnyAsync(c => c.Id == id, cancellationToken);
    }
}
