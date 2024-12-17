namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

public sealed class CardType
{
    public int Id { get; init; }
    public required string Name { get; init; }
}
