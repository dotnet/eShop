using MediatR;
using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.eShopOnContainers.Services.Ordering.API.Application.Exceptions;

namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands
{
    public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, bool>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<CompleteOrderCommandHandler> _logger;

        public CompleteOrderCommandHandler(IOrderRepository orderRepository, ILogger<CompleteOrderCommandHandler> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));

            var order = await _orderRepository.GetAsync(request.OrderId);
            
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                return false;
            }

            try
            {
                order.SetCompleted();
                
                _logger.LogInformation(
                    "Order {OrderId} status changed to {Status}", 
                    request.OrderId, 
                    order.OrderStatus);

                return await _orderRepository.UnitOfWork
                    .SaveEntitiesAsync(cancellationToken);
            }
            catch (OrderingDomainException ex)
            {
                _logger.LogError(
                    ex,
                    "Error completing order {OrderId}: {Message}", 
                    request.OrderId,
                    ex.Message);
                throw;
            }
        }
    }
} 