using Microsoft.Extensions.DependencyInjection;

using EShop.Features.Ordering.Extensions;
using EShop.Orchestration.Ordering.Extensions;

namespace EShop.Modules.Ordering.Extensions
{
    public static class OrderingModuleExtensions
    {
        public static IServiceCollection AddOrderingModule(this IServiceCollection services) { 
            services
                .AddOrderingFeatures()
                .AddOrderingOrchestration();

            return services;
        }
    }
}