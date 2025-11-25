using EShop.UseCases.Ordering.Contracts.Input;
using EShop.UseCases.Ordering.Contracts.Output;

namespace EShop.UseCases.Ordering.Ports
{
    public interface IOrderingPort
    {
        Task<CreateOrderOutput> CreateOrderAsync(CreateOrderInput input);
    }
}