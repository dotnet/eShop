using System.ComponentModel.DataAnnotations;

namespace EShop.Persistence.Models.Ordering
{
    public class OrderItem
    {
        [Required]
        public string ProductName { get; private set; }
        public string PictureUrl { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal Discount { get; private set; }
        public int Units { get; private set; }
        public int ProductId { get; private set; }
    }
}