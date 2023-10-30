namespace eShop.Ordering.API.Application.Commands;

public class CancelOrderCommand(int orderNumber) : IRequest<bool>
{
    [DataMember]
    public int OrderNumber { get; set; } = orderNumber;
}
