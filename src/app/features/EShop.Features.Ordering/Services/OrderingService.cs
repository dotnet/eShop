using DataArc.ObservR.Abstractions;

using EShop.Domain.Ordering.Policies.Context;
using EShop.Domain.Ordering.Policies.Contracts;
using EShop.Domain.Ordering.ValueObjects;

using EShop.Features.Ordering.Interfaces;
using EShop.UseCases.Ordering.Contracts.Input;

using EShop.UseCases.Ordering.Dtos;
using EShop.UseCases.Ordering.Ports;

namespace EShop.Features.Ordering.Services
{
    public class OrderingService(
        IOrderingPort orderingPort, 
        ICreateOrderPolicy createOrderPolicy, 
        IObservableEventHandler observableEventHandler) : IOrderingService
    {
        public async Task<OrderDto> CreateOrder(CreateOrderInput createOrderInput) {
            var address = new AddressValueObject(
                createOrderInput.Street, 
                createOrderInput.City, 
                createOrderInput.State, 
                createOrderInput.Country, 
                createOrderInput.ZipCode
            );

            var order = new OrderValueObject(
                createOrderInput.UserId,
                createOrderInput.UserName,
                address,
                createOrderInput.CardTypeId,
                createOrderInput.CardNumber,
                createOrderInput.CardSecurityNumber,
                createOrderInput.CardHolderName,
                createOrderInput.CardExpiration
            );

            var policyContext = new CreateOrderPolicyContext(order, address);
            var policyResult = createOrderPolicy.Apply(policyContext);

            if (policyResult.IsSuccess) {
                var orderResult = await orderingPort.CreateOrderAsync(createOrderInput);
                await observableEventHandler.DispatchAsync(policyResult.DomainEvents);
                return orderResult.OrderDto;
            }

            //Dataarc observR
            await observableEventHandler.DispatchAsync(policyResult.DomainEvents);
            return new();
        }

        public Task<List<OrderDto>> GetOrders()
        {
            throw new NotImplementedException();
        }
    }
}