using DataArc.OrchestratR.Abstractions;

using EShop.UseCases.Ordering.Dtos;

namespace EShop.UseCases.Ordering.Contracts.Output
{
    public class CreateOrderOutput : IOrchestratorOutput
    {
        public OrderDto OrderDto { get; set; }
        public List<OrderDto> OrderDtos { get; set; }
        public string ErrorMessage { get; set; }
    }
}