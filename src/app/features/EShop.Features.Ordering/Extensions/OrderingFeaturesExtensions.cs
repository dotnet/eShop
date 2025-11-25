using EShop.Features.Ordering.Interfaces;
using EShop.Features.Ordering.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Features.Ordering.Extensions
{
    public static  class OrderingFeaturesExtensions
    {
        public static IServiceCollection AddOrderingFeatures(this IServiceCollection services)
        {
            services.AddScoped<IOrderingService, OrderingService>();
            return services;
        }
    }
}