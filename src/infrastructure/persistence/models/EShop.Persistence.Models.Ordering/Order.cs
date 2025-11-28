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
        public DateTime OrderDate { get; set; }
        public int? BuyerId { get; set; }
        //public OrderStatus OrderStatus { get; set; }
        public int OrderStatus { get; set; }
        public string Description { get; set; }
        public int? PaymentId { get; set; }
    }
}