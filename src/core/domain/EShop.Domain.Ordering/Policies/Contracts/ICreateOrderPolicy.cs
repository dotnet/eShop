using EShop.Domain.Ordering.Policies.Context;
using EShop.Domain.SharedKernel.Policies;

namespace EShop.Domain.Ordering.Policies.Contracts
{
    public interface ICreateOrderPolicy : IPolicy<CreateOrderPolicyContext>
    {
        
    }
}