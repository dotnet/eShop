namespace eShop.Ordering.API.Application.Commands.CreateOrderDraft;

using eShop.Ordering.API.Application.Models;

public record CreateOrderDraftCommand(string BuyerId, IEnumerable<BasketItem> Items) : IRequest<OrderDraftDTO>;
