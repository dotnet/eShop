using System.Globalization;
using CommunityToolkit.Maui.Converters;

namespace eShop.ClientApp.Converters;

public class ItemsToHeightConverter : BaseConverterOneWay<int, int>
{
    private const int ItemHeight = 156;

    public override int DefaultConvertReturnValue { get; set; } = ItemHeight;

    public override int ConvertFrom(int value, CultureInfo culture)
    {
        return value * ItemHeight;
    }
}
