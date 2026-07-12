using System.Reflection;

using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.Seedwork;

using DomainCardType = eShop.Ordering.Domain.AggregatesModel.BuyerAggregate.CardType;
using Order = eShop.Ordering.Domain.AggregatesModel.OrderAggregate.Order;

namespace eShop.Ordering.FunctionalTests.Mocks.RepositoryMock;

internal sealed class OrderingRepositoryMockStore
{
    private int _nextOrderId;
    private int _nextOrderItemId;
    private int _nextBuyerId;
    private int _nextPaymentId;

    public List<Order> Orders { get; private set; } = [];
    public List<Buyer> Buyers { get; private set; } = [];
    public List<DomainCardType> CardTypes { get; private set; } = [];

    public Task ResetAsync()
    {
        Orders = [];
        Buyers = [];
        CardTypes =
        [
            new DomainCardType { Id = 1, Name = "Amex" },
            new DomainCardType { Id = 2, Name = "Visa" },
            new DomainCardType { Id = 3, Name = "MasterCard" }
        ];
        _nextOrderId = 0;
        _nextOrderItemId = 0;
        _nextBuyerId = 0;
        _nextPaymentId = 0;
        return Task.CompletedTask;
    }

    public void Commit(IReadOnlyList<Entity> tracked)
    {
        foreach (var order in tracked.OfType<Order>())
        {
            AssignOrderIds(order);
            UpsertOrder(order);
        }

        foreach (var buyer in tracked.OfType<Buyer>())
        {
            AssignBuyerIds(buyer);
            UpsertBuyer(buyer);
        }
    }

    private void AssignOrderIds(Order order)
    {
        if (order.IsTransient())
        {
            SetEntityId(order, ++_nextOrderId);
        }

        foreach (var item in order.OrderItems.Where(item => item.IsTransient()))
        {
            SetEntityId(item, ++_nextOrderItemId);
        }
    }

    private void AssignBuyerIds(Buyer buyer)
    {
        if (buyer.IsTransient())
        {
            SetEntityId(buyer, ++_nextBuyerId);
        }

        foreach (var payment in buyer.PaymentMethods.Where(payment => payment.IsTransient()))
        {
            SetEntityId(payment, ++_nextPaymentId);
        }
    }

    private void UpsertOrder(Order order)
    {
        var existingIndex = Orders.FindIndex(existing => existing.Id == order.Id);
        if (existingIndex >= 0)
        {
            Orders[existingIndex] = order;
        }
        else
        {
            Orders.Add(order);
        }
    }

    private void UpsertBuyer(Buyer buyer)
    {
        var existingIndex = Buyers.FindIndex(existing => existing.Id == buyer.Id);
        if (existingIndex >= 0)
        {
            Buyers[existingIndex] = buyer;
        }
        else
        {
            Buyers.Add(buyer);
        }
    }

    private static void SetEntityId(Entity entity, int id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(entity, id);
}
