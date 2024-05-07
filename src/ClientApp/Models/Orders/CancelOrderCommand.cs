namespace eShop.ClientApp.Models.Orders;

public class CancelOrderCommand
{
    public CancelOrderCommand(int orderNumber)
    {
        OrderNumber = orderNumber;
    }

    public int OrderNumber { get; }
}
