using Inked.WebAppComponents.Services;

namespace Inked.WebApp.Services;

public class ProductImageUrlProvider : IProductImageUrlProvider
{
    public string GetProductImageUrl(int productId)
    {
        return $"product-images/{productId}?api-version=2.0";
    }
}
