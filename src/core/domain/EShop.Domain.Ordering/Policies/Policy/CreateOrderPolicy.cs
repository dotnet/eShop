using EShop.Domain.Ordering.Events;
using EShop.Domain.Ordering.Policies.Context;
using EShop.Domain.Ordering.Policies.Contracts;
using EShop.Domain.Ordering.ValueObjects;
using EShop.Domain.SharedKernel.Policies;
using System.Net;

namespace EShop.Domain.Ordering.Policies.Policy
{
    public class CreateOrderPolicy : ICreateOrderPolicy
    {
        public PolicyResult Apply(CreateOrderPolicyContext context)
        {
            //if (context.CanCreateOrder()){
            //    return PolicyResult.Success(new OrderStartedDomainEvent(new OrderValueObject(
            //    createOrderInput.UserId,
            //    createOrderInput.UserName,
            //    address,
            //    createOrderInput.CardTypeId,
            //    createOrderInput.CardNumber,
            //    createOrderInput.CardSecurityNumber,
            //    createOrderInput.CardHolderName,
            //    createOrderInput.CardExpiration
            //)));
            //}

            //return PolicyResult.Fail("Order was cancelled", new OrderCancelledDomainEvent());
            return PolicyResult.Success();
        }
    }
}