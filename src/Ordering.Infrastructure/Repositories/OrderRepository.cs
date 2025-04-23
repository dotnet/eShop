namespace eShop.Ordering.Infrastructure.Repositories;

public class OrderRepository
    : IOrderRepository
{
    private readonly OrderingContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public OrderRepository(OrderingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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

    public async Task<Product> GetProductAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);

        if (product != null)
        {
            await _context.Entry(product).Collection(p => p.ProductDetails).LoadAsync();
        }

        return product;
    }

    public Product AddProduct(Product product)
    {
        return _context.Products.Add(product).Entity;
    }

    public void UpdateProduct(Product product)
    {
        _context.Entry(product).State = EntityState.Modified;
    }
}