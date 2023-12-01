namespace eShop.Ordering.API.Application.Queries;

public class OrderQueries(IOrderRepository orderRepository, OrderingContext context)
    : IOrderQueries
{
    public async Task<Order> GetOrderAsync(int id)
    {
        var order = await orderRepository.GetAsync(id);
        if (order is null)
            throw new KeyNotFoundException();

        return new Order
        {
            ordernumber = order.Id,
            date = order.GetOrderDate(),
            description = order.GetDescription(),
            city = order.Address.City,
            country = order.Address.Country,
            state = order.Address.State,
            street = order.Address.Street,
            zipcode = order.Address.ZipCode,
            status = order.OrderStatus.Name,
            total = order.GetTotal(),
            orderitems = order.OrderItems.Select(oi => new Orderitem
            {
                productname = oi.GetOrderItemProductName(),
                units = oi.GetUnits(),
                unitprice = (double)oi.GetUnitPrice(),
                pictureurl = oi.GetPictureUri()
            }).ToList()
        };

    }

    public async Task<IEnumerable<OrderSummary>> GetOrdersFromUserAsync(string userId)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderStatus)
            .Join(context.Buyers,
                o => o.BuyerId,  
                b => b.Id,       
                (order, buyer) => new { order, buyer })
            .Where(ob => ob.buyer.IdentityGuid == userId)  
            .Select(ob => new OrderSummary
            {
                ordernumber = ob.order.Id,
                date = ob.order.GetOrderDate(),
                status = ob.order.OrderStatus.Name,
                total = (double)ob.order.GetTotal()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<CardType>> GetCardTypesAsync() => 
        await context.CardTypes.Select(c=> new CardType { Id = c.Id, Name = c.Name }).ToListAsync();
}
