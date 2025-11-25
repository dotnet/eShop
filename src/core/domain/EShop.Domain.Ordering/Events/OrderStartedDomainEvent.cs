using EShop.Domain.Ordering.Entities;
using EShop.Domain.Ordering.ValueObjects;
using EShop.Domain.SharedKernel.Events;

namespace EShop.Domain.Ordering.Events
{
    public record class OrderStartedDomainEvent(
    OrderValueObject Order,
    string UserId,
    string UserName,
    int CardTypeId,
    string CardNumber,
    string CardSecurityNumber,
    string CardHolderName,
    DateTime CardExpiration) : IDomainEvent;
}