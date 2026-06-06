
namespace eShop.Ordering.Domain.Events;

/// <summary>
/// Event used when an order is created
/// </summary>
public record class OrderStartedDomainEvent(
    Order Order, 
    string UserId,
    string UserName,
    string PaymentMethodId) : INotification;
