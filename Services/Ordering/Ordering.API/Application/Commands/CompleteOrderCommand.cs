using MediatR;

namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands
{
    public class CompleteOrderCommand : IRequest<bool>
    {
        public int OrderId { get; private set; }

        public CompleteOrderCommand(int orderId)
        {
            OrderId = orderId;
        }
    }
} 