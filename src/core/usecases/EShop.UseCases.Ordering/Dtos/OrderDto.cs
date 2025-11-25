using EShop.Domain.Ordering.Enums;

namespace EShop.UseCases.Ordering.Dtos
{
    public class OrderDto
    {
        public OrderStatus OrderStatus { get; set; }
        public string Description { get; set; }
        public int? PaymentId { get; set; }
        public IEnumerable<OrderItemDTO> OrderItems { get; set; }
    }
}