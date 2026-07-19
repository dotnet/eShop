namespace eShop.Coupon.Application.Services;

using eShop.Coupon.Application.Dtos;
using eShop.Coupon.Application.Interfaces;
using eShop.Coupon.Domain.Entities;
using eShop.Coupon.Domain.Exceptions;
using AutoMapper;

public class CouponService
{
    private readonly ICouponDataAccess _dataAccess;
    private readonly IMapper _mapper;

    public CouponService(ICouponDataAccess dataAccess, IMapper mapper)
    {
        _dataAccess = dataAccess;
        _mapper = mapper;
    }

    public async Task<CouponDto> CreateCouponAsync(CreateCouponRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateCouponRequest(request);

        var existingCoupon = await _dataAccess.GetByCodeAsync(request.Code, cancellationToken);
        if (existingCoupon != null)
        {
            throw new CouponException($"Coupon with code '{request.Code}' already exists.");
        }

        var coupon = _mapper.Map<Coupon>(request);
        await _dataAccess.CreateAsync(coupon, cancellationToken);

        return _mapper.Map<CouponDto>(coupon);
    }

    public async Task<CouponDto> GetCouponByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var coupon = await _dataAccess.GetByIdAsync(id, cancellationToken)
            ?? throw new CouponException($"Coupon with ID '{id}' not found.");

        return _mapper.Map<CouponDto>(coupon);
    }

    public async Task<CouponDto> GetCouponByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var coupon = await _dataAccess.GetByCodeAsync(code, cancellationToken)
            ?? throw new CouponException($"Coupon with code '{code}' not found.");

        return _mapper.Map<CouponDto>(coupon);
    }

    public async Task<CouponDto> UpdateCouponAsync(Guid id, UpdateCouponRequest request, CancellationToken cancellationToken = default)
    {
        var coupon = await _dataAccess.GetByIdAsync(id, cancellationToken)
            ?? throw new CouponException($"Coupon with ID '{id}' not found.");

        if (request.IsActive.HasValue)
        {
            coupon.IsActive = request.IsActive.Value;
        }

        if (request.MaxUsageCount.HasValue)
        {
            if (request.MaxUsageCount < coupon.CurrentUsageCount)
            {
                throw new CouponException("MaxUsageCount cannot be less than current usage count.");
            }
            coupon.MaxUsageCount = request.MaxUsageCount.Value;
        }

        coupon.UpdatedAt = DateTime.UtcNow;
        await _dataAccess.UpdateAsync(coupon, cancellationToken);

        return _mapper.Map<CouponDto>(coupon);
    }

    public async Task DeleteCouponAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _dataAccess.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            throw new CouponException($"Coupon with ID '{id}' not found.");
        }

        await _dataAccess.DeleteAsync(id, cancellationToken);
    }

    public async Task<decimal> ApplyAndIncrementUsageAsync(string code, decimal orderTotal, CancellationToken cancellationToken = default)
    {
        var coupon = await _dataAccess.GetByCodeAsync(code, cancellationToken)
            ?? throw new CouponException($"Coupon with code '{code}' not found.");

        if (!coupon.IsValid())
        {
            throw new CouponException("Coupon is not valid or has expired.");
        }

        if (!coupon.CanBeApplied(orderTotal))
        {
            throw new CouponException($"Coupon cannot be applied. Minimum order amount required: {coupon.MinimumOrderAmount}");
        }

        var discount = coupon.CalculateDiscount(orderTotal, 1);
        coupon.IncrementUsage();
        await _dataAccess.UpdateAsync(coupon, cancellationToken);

        return discount;
    }

    private static void ValidateCreateCouponRequest(CreateCouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new CouponException("Coupon code is required.");
        }

        if (request.DiscountValue <= 0)
        {
            throw new CouponException("Discount value must be greater than zero.");
        }

        if (request.MinimumOrderAmount.HasValue && request.MinimumOrderAmount < 0)
        {
            throw new CouponException("Minimum order amount cannot be negative.");
        }

        if (request.ExpiryDate.HasValue && request.ExpiryDate < DateTime.UtcNow)
        {
            throw new CouponException("Expiry date must be in the future.");
        }

        if (request.MaxUsageCount.HasValue && request.MaxUsageCount <= 0)
        {
            throw new CouponException("Max usage count must be greater than zero.");
        }
    }
}
