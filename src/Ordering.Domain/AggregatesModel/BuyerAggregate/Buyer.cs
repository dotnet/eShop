using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

public class Buyer
    : Entity, IAggregateRoot
{
    [Required]
    public string IdentityGuid { get; private set; }

    public string Name { get; private set; }

    private List<PaymentMethod> _paymentMethods;

    public IEnumerable<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    protected Buyer()
    {

        _paymentMethods = new List<PaymentMethod>();
    }

    public Buyer(string identity, string name) : this()
    {
        IdentityGuid = !string.IsNullOrWhiteSpace(identity) ? identity : throw new ArgumentNullException(nameof(identity));
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));
    }

    public PaymentMethod VerifyOrAddPaymentMethod(string alias, string paymentMethodId, int orderId)
    {
        var existingPayment = _paymentMethods
            .SingleOrDefault(p => p.IsEqualTo(paymentMethodId));

        if (existingPayment != null)
        {
            AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, existingPayment, orderId));

            return existingPayment;
        }

        var payment = new PaymentMethod(alias, paymentMethodId);

        _paymentMethods.Add(payment);

        AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, payment, orderId));

        return payment;
    }
}
