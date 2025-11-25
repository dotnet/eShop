using EShop.UseCases.Ordering.Contracts.Input;
using EShop.UseCases.Ordering.Dtos;

namespace EShop.Features.Ordering.Interfaces
{
    public interface IOrderingService
    {
        Task<OrderDto> CreateOrder(CreateOrderInput createOrderInput);
        Task<List<OrderDto>> GetOrders();
    }
}