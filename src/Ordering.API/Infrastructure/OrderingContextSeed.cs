namespace eShop.Ordering.API.Infrastructure;

using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

public class OrderingContextSeed: IDbSeeder<OrderingContext>
{
    public async Task SeedAsync(OrderingContext context)
    {

        if (!context.CardTypes.Any())
        {
            context.CardTypes.AddRange(GetPredefinedCardTypes());

            await context.SaveChangesAsync();
        }

        if (!context.OrderStatus.Any())
        {
            context.OrderStatus.AddRange(GetPredefinedOrderStatus());
        }

        await context.SaveChangesAsync();
    }

    private static IEnumerable<CardType> GetPredefinedCardTypes()
    {
        return Enumeration.GetAll<CardType>();
    }

    private static List<OrderStatus> GetPredefinedOrderStatus()
    {
        return [
            OrderStatus.Submitted,
            OrderStatus.AwaitingValidation,
            OrderStatus.StockConfirmed,
            OrderStatus.Paid,
            OrderStatus.Shipped,
            OrderStatus.Cancelled
        ];
    }
}
