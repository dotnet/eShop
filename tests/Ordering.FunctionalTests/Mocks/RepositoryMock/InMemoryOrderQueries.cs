using eShop.Ordering.API.Application.Queries;

using QueryCardType = eShop.Ordering.API.Application.Queries.CardType;
using QueryOrder = eShop.Ordering.API.Application.Queries.Order;
using QueryOrderItem = eShop.Ordering.API.Application.Queries.Orderitem;
using QueryOrderSummary = eShop.Ordering.API.Application.Queries.OrderSummary;

namespace eShop.Ordering.FunctionalTests.Mocks.RepositoryMock;

internal sealed class InMemoryOrderQueries(OrderingRepositoryMockStore store) : IOrderQueries
{
    public Task<QueryOrder> GetOrderAsync(int id)
    {
        var order = store.Orders.SingleOrDefault(existing => existing.Id == id)
            ?? throw new KeyNotFoundException();

        return Task.FromResult(new QueryOrder
        {
            OrderNumber = order.Id,
            Date = order.OrderDate,
            Description = order.Description,
            City = order.Address.City,
            Country = order.Address.Country,
            State = order.Address.State,
            Street = order.Address.Street,
            Zipcode = order.Address.ZipCode,
            Status = order.OrderStatus.ToString(),
            Total = order.GetTotal(),
            OrderItems = order.OrderItems.Select(item => new QueryOrderItem
            {
                ProductName = item.ProductName,
                Units = item.Units,
                UnitPrice = (double)item.UnitPrice,
                PictureUrl = item.PictureUrl
            }).ToList()
        });
    }

    public Task<IEnumerable<QueryOrderSummary>> GetOrdersFromUserAsync(string userId)
    {
        var summaries = store.Orders
            .Where(order => store.Buyers.Any(buyer => buyer.Id == order.BuyerId && buyer.IdentityGuid == userId))
            .Select(order => new QueryOrderSummary
            {
                OrderNumber = order.Id,
                Date = order.OrderDate,
                Status = order.OrderStatus.ToString(),
                Total = (double)order.OrderItems.Sum(item => item.UnitPrice * item.Units)
            })
            .ToList();

        return Task.FromResult<IEnumerable<QueryOrderSummary>>(summaries);
    }

    public Task<IEnumerable<QueryCardType>> GetCardTypesAsync() =>
        Task.FromResult<IEnumerable<QueryCardType>>(store.CardTypes.Select(cardType => new QueryCardType
        {
            Id = cardType.Id,
            Name = cardType.Name
        }).ToList());
}
