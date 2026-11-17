using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

public class PaymentMethod : Entity
{
    [Required]
    private string _alias;
    [Required]
    private string _paymentToken;

    protected PaymentMethod() { }

    public PaymentMethod(string alias, string paymentToken)
    {
        _alias = alias;
        _paymentToken = !string.IsNullOrWhiteSpace(paymentToken) ? paymentToken : throw new OrderingDomainException(nameof(paymentToken));
    }

    public bool IsEqualTo(string paymentToken)
    {
        return _paymentToken == paymentToken;
    }
}
