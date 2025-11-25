using DataArc.OrchestratR.Abstractions;
using EShop.UseCases.Ordering.Contracts.Input;
using EShop.UseCases.Ordering.Contracts.Output;
using EShop.UseCases.Ordering.Ports;

using DataArc.OrchestratR;
using EShop.Orchestration.Ordering.Orchestrators;

namespace EShop.Modules.Ordering.Adapters
{
    public class OrderingAdapter(IOrchestratorHandler orchestratorHandler) : IOrderingPort
    {
        public async Task<CreateOrderOutput> CreateOrderAsync(CreateOrderInput input) 
            => await orchestratorHandler.OrchestrateAsync<CreateOrderOrchestrator, CreateOrderOutput>(input, new CreateOrderOutput());
    }
}