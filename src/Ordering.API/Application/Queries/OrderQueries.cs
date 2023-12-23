namespace eShop.Ordering.API.Application.Queries;

public class OrderQueries(OrderingContext context)
    : IOrderQueries
{
    public async Task<Order> GetOrderAsync(int id)
    {
        var order = await context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.Id == id);
      
        if (order is null)
            throw new KeyNotFoundException();

        return new Order
        {
            ordernumber = order.Id,
            date = order.OrderDate,
            description = order.Description,
            city = order.Address.City,
            country = order.Address.Country,
            state = order.Address.State,
            street = order.Address.Street,
            zipcode = order.Address.ZipCode,
            status = order.OrderStatus.Name,
            total = order.GetTotal(),
            orderitems = order.OrderItems.Select(oi => new Orderitem
            {
                productname = oi.ProductName,
                units = oi.Units,
                unitprice = (double)oi.UnitPrice,
                pictureurl = oi.PictureUrl
            }).ToList()
        };
    }

    public async Task<IEnumerable<OrderSummary>> GetOrdersFromUserAsync(string userId)
    {
        return await context.Orders
            .Join(context.Buyers,
                o => o.BuyerId,  
                b => b.Id,       
                (order, buyer) => new { order, buyer })
            .Where(ob => ob.buyer.IdentityGuid == userId)  
            .Select(ob => new OrderSummary
            {
                ordernumber = ob.order.Id,
                date = ob.order.OrderDate,
                status = ob.order.OrderStatus.Name,
                total =(double) ob.order.OrderItems.Sum(oi => oi.UnitPrice* oi.Units)
            })
            .AsSplitQuery()
            .ToListAsync();
    } 

    public async Task<IEnumerable<CardType>> GetCardTypesAsync() => 
        await context.CardTypes.Select(c=> new CardType { Id = c.Id, Name = c.Name }).ToListAsync();
}
