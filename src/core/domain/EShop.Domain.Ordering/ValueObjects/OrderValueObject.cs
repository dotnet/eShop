using EShop.Domain.Ordering.Entities;
using EShop.Domain.Ordering.Enums;
using EShop.Domain.SharedKernel.ValueObjects;

namespace EShop.Domain.Ordering.ValueObjects
{
    public class OrderValueObject : ValueObject
    {
        public int? PaymentId { get; set; }
        public AddressValueObject Address { get; private set; }
        public int? BuyerId { get; private set; }
        public Buyer Buyer { get; }
        public OrderStatus OrderStatus { get; private set; }
        public string Description { get; private set; }
        public DateTime OrderDate { get; set; }

        public OrderValueObject(string userId, string userName, AddressValueObject address, int cardTypeId, string cardNumber, string cardSecurityNumber,
                string cardHolderName, DateTime cardExpiration, int? buyerId = null, int? paymentMethodId = null)
        {
            BuyerId = buyerId;
            PaymentId = paymentMethodId;
            OrderStatus = OrderStatus.Submitted;
            OrderDate = DateTime.UtcNow;
            Address = address;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return BuyerId;
            yield return PaymentId;
            yield return OrderStatus;
            yield return OrderDate;
            yield return Address;
        }
    }
}