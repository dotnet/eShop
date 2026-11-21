namespace eShop.Ordering.Infrastructure.Repositories;

public class OrderRepository
    : IOrderRepository
{
    private readonly OrderingContext _context;

    public OrderRepository(OrderingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveEntitiesAsync(cancellationToken);
    }

    public Order Add(Order order)
    {
        return _context.Orders.Add(order).Entity;

    }

    public async Task<Order> GetAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order != null)
        {
            await _context.Entry(order)
                .Collection(i => i.OrderItems).LoadAsync();
        }

        return order;
    }

    public void Update(Order order)
    {
        _context.Entry(order).State = EntityState.Modified;
    }
}
