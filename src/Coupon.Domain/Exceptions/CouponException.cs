namespace eShop.Coupon.Domain.Exceptions;

public class CouponException : Exception
{
    public CouponException(string message)
        : base(message)
    {
    }

    public CouponException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
