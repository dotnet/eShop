using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace eShop.Basket.FunctionalTests.Base;

public class BasketScenarioBase
{
    private const string BaseUrl = "api/v1/basket";

    public TestServer CreateServer()
    {
        var factory = new BasketApplication();
        return factory.Server;
    }

    public static class Get
    {
        public static string GetBasket(int id) => $"{BaseUrl}/{id}";

        public static string GetBasketByCustomer(string customerId) => $"{BaseUrl}/{customerId}";
    }

    public static class Post
    {
        public const string Basket = $"{BaseUrl}/";

        public const string CheckoutOrder = $"{BaseUrl}/checkout";
    }

    private class BasketApplication : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter, AuthStartupFilter>();
            });

            builder.ConfigureAppConfiguration(c =>
            {
                var directory = Path.GetDirectoryName(typeof(BasketScenarioBase).Assembly.Location)!;

                c.AddJsonFile(Path.Combine(directory, "appsettings.Basket.json"), optional: false);
            });

            return base.CreateHost(builder);
        }

        private class AuthStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.UseMiddleware<AutoAuthorizeMiddleware>();

                    next(app);
                };
            }
        }
    }
}
