using eShop.WebAppComponents.Services;

namespace eShop.HybridApp.Services;

public class ProductImageUrlProvider : IProductImageUrlProvider
{
    public string GetProductImageUrl(int productId) 
        => $"{MauiProgram.ProductImageHostUrl}/product-images/{productId}";
}
