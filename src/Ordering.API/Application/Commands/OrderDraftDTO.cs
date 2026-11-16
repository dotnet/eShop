namespace eShop.Ordering.API.Application.Commands;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

public record OrderDraftDTO
{
    public IEnumerable<OrderItemDTO> OrderItems { get; init; }
    public decimal Total { get; init; }

    public static OrderDraftDTO FromOrder(Order order)
    {
        return new OrderDraftDTO()
        {
            OrderItems = order.OrderItems.Select(oi => new OrderItemDTO
            {
                Discount = oi.Discount,
                ProductId = oi.ProductId,
                UnitPrice = oi.UnitPrice,
                PictureUrl = oi.PictureUrl,
                Units = oi.Units,
                ProductName = oi.ProductName
            }),
            Total = order.GetTotal()
        };
    }
}

public record OrderItemDTO
{
    public int ProductId { get; init; }

    public string ProductName { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal Discount { get; init; }

    public int Units { get; init; }

    public string PictureUrl { get; init; }
}
