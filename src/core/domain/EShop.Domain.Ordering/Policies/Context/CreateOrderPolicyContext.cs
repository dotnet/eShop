using EShop.Domain.Ordering.ValueObjects;

namespace EShop.Domain.Ordering.Policies.Context
{
    public class CreateOrderPolicyContext
    {
        private OrderValueObject Order { get; set; }
        private AddressValueObject Address { get; set; }

        public CreateOrderPolicyContext(OrderValueObject order, AddressValueObject address)
        {
            Order = order;
            Address = address;
        }

        public bool CanCreateOrder() {
            //MORE RULES HERE
            if (Order is null || Address == null) 
                return false; 
            return true;
        }
    }
}