namespace Inked.Ordering.API.Application.Commands;

public record CreateOrderDraftCommand(string BuyerId, IEnumerable<BasketItem> Items) : IRequest<OrderDraftDTO>;
