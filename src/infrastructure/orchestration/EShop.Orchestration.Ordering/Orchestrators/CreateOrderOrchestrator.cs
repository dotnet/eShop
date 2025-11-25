using DataArc.Abstractions;
using DataArc.OrchestratR;

using EShop.Persistence.Contexts.Ordering;
using EShop.Persistence.Models.Ordering;
using EShop.UseCases.Ordering.Contracts.Input;
using EShop.UseCases.Ordering.Contracts.Output;
using EShop.UseCases.Ordering.Dtos;

namespace EShop.Orchestration.Ordering.Orchestrators
{
    public class CreateOrderOrchestrator : Orchestrator<CreateOrderInput, CreateOrderOutput>
    {
        readonly IAsyncDatabaseCommandBuilder _asyncDatabaseCommandBuilder;
        readonly IAsyncDatabaseQueryBuilder _asyncDatabaseQueryBuilder;

        public CreateOrderOrchestrator(
            IAsyncDatabaseCommandBuilder asyncDatabaseCommandBuilder,
            IAsyncDatabaseQueryBuilder asyncDatabaseQueryBuilder
            )
        {
            _asyncDatabaseCommandBuilder = asyncDatabaseCommandBuilder;
            _asyncDatabaseQueryBuilder = asyncDatabaseQueryBuilder;
        }

        public override async Task<CreateOrderOutput> ExecuteAsync(CreateOrderInput input, CreateOrderOutput output)
        {
            var createOrderCommand = await _asyncDatabaseCommandBuilder
                .UseCommandContext<OrderingContext>()
                .Add(new Order()
                {
                    // For simplicity, only a few fields are set. In a real scenario, map all necessary fields.
                }
                ).AddRange(new List<OrderItem>() { new OrderItem() { 
                    // For simplicity, only a few fields are set. In a real scenario, map all necessary fields.
                }})
                .BuildAsync();

            var result = await createOrderCommand.ExecuteAsync();
            if (!result.Success) { 
                output.ErrorMessage = "Failed";
            }

            var orders = await _asyncDatabaseQueryBuilder
                .UseQueryContext<OrderingContext>()
                .ReadWhereAsync<Order>(o => o.BuyerId.ToString() == input.UserId);

            if (orders != null) { 
                var order = orders.FirstOrDefault();
                output.OrderDto = new OrderDto()
                {
                    OrderStatus = order.OrderStatus,
                    Description = order.Description,
                    PaymentId = order.PaymentId,
                };
            }

            return output;
        }
    }
}