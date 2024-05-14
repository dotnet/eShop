using System.ComponentModel.DataAnnotations;
using eShop.ClientApp.Models.Orders;

namespace eShop.ClientApp.Models.Basket;

public class OrderCheckout
{
    [Required]
    public string City { get; set; }
    [Required]
    public string Street { get; set; }
    [Required]
    public string State { get; set; }
    [Required]
    public string Country { get; set; }

    public string ZipCode { get; set; }
    [Required]
    public string CardNumber { get; set; }
    [Required]
    public string CardHolderName { get; set; }

    [Required]
    public DateTime CardExpiration { get; set; }

    [Required]
    public string CardSecurityNumber { get; set; }

    public int CardTypeId { get; set; }

    public string Buyer { get; set; }

    public IList<OrderItem> Items { get; set; }
    
    [Required]
    public Guid RequestId { get; set; }
}
