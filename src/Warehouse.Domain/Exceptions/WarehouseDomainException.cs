namespace eShop.Warehouse.Domain.Exceptions;

public class WarehouseDomainException : Exception
{
    public WarehouseDomainException()
    { }

    public WarehouseDomainException(string message)
        : base(message)
    { }

    public WarehouseDomainException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
