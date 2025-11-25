using EShop.Domain.Ordering.Entities;
using EShop.Domain.SharedKernel.Events;

namespace EShop.Domain.Ordering.Events;

public class BuyerAndPaymentMethodVerifiedDomainEvent
    : IDomainEvent
{
    public Buyer Buyer { get; private set; }
    public PaymentMethod Payment { get; private set; }
    public int OrderId { get; private set; }

    public BuyerAndPaymentMethodVerifiedDomainEvent(Buyer buyer, PaymentMethod payment, int orderId)
    {
        Buyer = buyer;
        Payment = payment;
        OrderId = orderId;
    }
}