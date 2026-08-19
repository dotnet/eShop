using System.Globalization;

namespace eShop.WebAppComponents.Services;

public static class PriceFormatter
{
    // Formats a monetary amount with thousands separators and two decimals
    // (e.g. 1234.5m -> "1,234.50") using invariant culture to match the "$" prefix.
    public static string ToPriceDisplay(this decimal value)
        => value.ToString("N2", CultureInfo.InvariantCulture);
}
