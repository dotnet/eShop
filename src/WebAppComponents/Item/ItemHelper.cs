using Inked.WebAppComponents.Catalog;

namespace Inked.WebAppComponents.Item;

public static class ItemHelper
{
    public static string Url(CatalogItem item)
    {
        return $"item/{item.Id}";
    }
}
