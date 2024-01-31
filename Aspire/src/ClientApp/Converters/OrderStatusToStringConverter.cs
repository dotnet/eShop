using System.Globalization;
using CommunityToolkit.Maui.Converters;
using eShop.ClientApp.Models.Orders;

namespace eShop.ClientApp.Converters;

public class OrderStatusToStringConverter : BaseConverterOneWay<OrderStatus, string>
{
    public override string DefaultConvertReturnValue { get; set; } = string.Empty;

    public override string ConvertFrom(OrderStatus value, CultureInfo culture)
    {
        return value switch
        {
            OrderStatus.AwaitingValidation => "AWAITING VALIDATION",
            OrderStatus.Cancelled => "CANCELLED",
            OrderStatus.Paid => "PAID",
            OrderStatus.Shipped => "SHIPPED",
            OrderStatus.StockConfirmed => "STOCK CONFIRMED",
            OrderStatus.Submitted => "SUBMITTED",
            _ => string.Empty
        };
    }
}

