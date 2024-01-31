namespace eShop.Ordering.API.Application.Queries;

public class OrderQueries(NpgsqlDataSource dataSource)
    : IOrderQueries
{
    public async Task<Order> GetOrderAsync(int id)
    {
        using var connection = dataSource.OpenConnection();

        var result = await connection.QueryAsync<dynamic>("""
            SELECT o."Id" AS ordernumber, o."OrderDate" AS date, o."Description" AS description, o."Address_City" AS city,
                o."Address_Country" AS country, o."Address_State" AS state, o."Address_Street" AS street,
                o."Address_ZipCode" AS zipcode, o."OrderStatus" AS status, oi."ProductName" AS productname, oi."Units" AS units,
                oi."UnitPrice" AS unitprice, oi."PictureUrl" AS pictureurl
            FROM ordering.Orders AS o
            LEFT JOIN ordering."orderItems" AS oi ON o."Id" = oi."OrderId"
            WHERE o."Id" = @id
            """,
            new { id });

        if (result.AsList().Count == 0)
            throw new KeyNotFoundException();

        return MapOrderItems(result);
    }

    public async Task<IEnumerable<OrderSummary>> GetOrdersFromUserAsync(string userId)
    {
        using var connection = dataSource.OpenConnection();

        return await connection.QueryAsync<OrderSummary>("""
            SELECT o."Id" AS ordernumber, o."OrderDate" AS date, o."OrderStatus" AS status, SUM(oi."Units" * oi."UnitPrice") AS total
            FROM ordering.orders AS o
            LEFT JOIN ordering."orderItems" AS oi ON o."Id" = oi."OrderId"
            LEFT JOIN ordering.buyers AS ob ON o."BuyerId" = ob."Id"
            WHERE ob."IdentityGuid" = @userId
            GROUP BY o."Id", o."OrderDate", o."OrderStatus"
            ORDER BY o."Id"
            """,
            new { userId });
    }

    public async Task<IEnumerable<CardType>> GetCardTypesAsync()
    {
        using var connection = dataSource.OpenConnection();

        return await connection.QueryAsync<CardType>("SELECT * FROM ordering.cardtypes");
    }

    private Order MapOrderItems(dynamic result)
    {
        var order = new Order
        {
            ordernumber = result[0].ordernumber,
            date = result[0].date,
            status = result[0].status,
            description = result[0].description,
            street = result[0].street,
            city = result[0].city,
            state = result[0].state,
            zipcode = result[0].zipcode,
            country = result[0].country,
            orderitems = new List<Orderitem>(),
            total = 0
        };

        foreach (dynamic item in result)
        {
            var orderitem = new Orderitem
            {
                productname = item.productname,
                units = item.units,
                unitprice = (double)item.unitprice,
                pictureurl = item.pictureurl
            };

            order.total += item.units * item.unitprice;
            order.orderitems.Add(orderitem);
        }

        return order;
    }
}
