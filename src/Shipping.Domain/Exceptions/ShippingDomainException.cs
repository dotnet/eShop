namespace eShop.Shipping.Domain.Exceptions;

public class ShippingDomainException : Exception
{
    public ShippingDomainException()
    { }

    public ShippingDomainException(string message)
        : base(message)
    { }

    public ShippingDomainException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
