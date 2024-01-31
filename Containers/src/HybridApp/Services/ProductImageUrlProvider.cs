using eShop.WebAppComponents.Services;

namespace eShop.HybridApp.Services;

public class ProductImageUrlProvider : IProductImageUrlProvider
{
    public string GetProductImageUrl(int productId)
        => $"{MauiProgram.MobileBffCatalogBaseUrl}api/v1/catalog/items/{productId}/pic";
}
