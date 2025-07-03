namespace eShop.Catalog.API;

public class TradeGeckoOptions
{
    public const string SectionName = "TradeGecko";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
} 