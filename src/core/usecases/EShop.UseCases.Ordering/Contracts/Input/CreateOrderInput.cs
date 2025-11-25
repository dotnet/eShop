using DataArc.OrchestratR.Abstractions;

using EShop.UseCases.Ordering.Dtos;

namespace EShop.UseCases.Ordering.Contracts.Input
{
    public class CreateOrderInput : IOrchestratorInput
    {
        public string UserId { get; private set; }
        public string UserName { get; private set; }
        public string City { get; private set; }
        public string Street { get; private set; }
        public string State { get; private set; }
        public string Country { get; private set; }
        public string ZipCode { get; private set; }
        public string CardNumber { get; private set; }
        public string CardHolderName { get; private set; }
        public DateTime CardExpiration { get; private set; }
        public string CardSecurityNumber { get; private set; }
        public int CardTypeId { get; private set; }

        public List<OrderItemDTO> OrderItems { get; private set; }
    }
}