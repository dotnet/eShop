using EShop.Domain.Ordering.Enums;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EShop.Persistence.Models.Ordering
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime OrderDate { get; private set; }
        [Required]
        public Address Address { get; private set; }
        public int? BuyerId { get; private set; }
        public Buyer Buyer { get; }
        public OrderStatus OrderStatus { get; private set; }
        public string Description { get; private set; }
        public int? PaymentId { get; private set; }
    }
}