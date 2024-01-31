using eShop.ClientApp.Models.Orders;

namespace eShop.ClientApp.Models.User;

public class PaymentInfo
{
    public Guid Id { get; set; }
    public string CardNumber { get; set; }
    public string SecurityNumber { get; set; }
    public int ExpirationMonth { get; set; }
    public int ExpirationYear { get; set; }
    public string CardHolderName { get; set; }
    public CardType CardType { get; set; }
    public string Expiration { get; set; }
}
