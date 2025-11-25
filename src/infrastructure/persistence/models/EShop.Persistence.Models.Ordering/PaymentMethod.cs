using System.ComponentModel.DataAnnotations.Schema;

namespace EShop.Persistence.Models.Ordering
{
    public class PaymentMethod
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public CardType CardType { get; set; }
    }
}