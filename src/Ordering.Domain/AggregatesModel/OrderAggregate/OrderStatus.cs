namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

public enum OrderStatus
{
    Submitted = 1,
    AwaitingValidation = 2,
    StockConfirmed = 3,
    Paid = 4,
    Shipped = 5,
    Cancelled = 6
}
