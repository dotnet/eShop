using DataArc.OrchestratR;
using EShop.Orchestration.Ordering.Orchestrators;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Orchestration.Ordering.Extensions
{
    public static class OrderingOrchestrationExtensions
    {
        public static IServiceCollection AddOrderingOrchestration(this IServiceCollection services)
        {
            services.AddDataArcOrchestration(
                orch =>
                {
                    orch.AddOrchestrator<CreateOrderOrchestrator>();
                });

            return services;
        }
    }
}