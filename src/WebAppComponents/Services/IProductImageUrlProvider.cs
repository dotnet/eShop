using Inked.WebAppComponents.Catalog;

namespace Inked.WebAppComponents.Services;

public interface IProductImageUrlProvider
{
    string GetProductImageUrl(CatalogItem item)
    {
        return GetProductImageUrl(item.Id);
    }

    string GetProductImageUrl(int productId);
}
