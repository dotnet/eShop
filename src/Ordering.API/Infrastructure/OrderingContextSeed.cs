namespace eShop.Ordering.API.Infrastructure;

using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

public class OrderingContextSeed: IDbSeeder<OrderingContext>
{
    public Task SeedAsync(OrderingContext context) => Task.CompletedTask;
}
