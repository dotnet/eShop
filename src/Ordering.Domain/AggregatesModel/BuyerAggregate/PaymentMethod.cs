using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

public class PaymentMethod : Entity
{
    [Required]
    private string _alias;
    [Required]
    private string _paymentMethodId;

    protected PaymentMethod() { }

    public PaymentMethod(string alias, string paymentMethodId)
    {
        _alias = alias;
        _paymentMethodId = !string.IsNullOrWhiteSpace(paymentMethodId) ? paymentMethodId : throw new OrderingDomainException(nameof(paymentMethodId));
    }

    public bool IsEqualTo(string paymentMethodId)
    {
        return _paymentMethodId == paymentMethodId;
    }
}
