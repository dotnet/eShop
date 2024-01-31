using Pgvector;

namespace eShop.Catalog.API.Services;

public interface ICatalogAI
{
    bool IsEnabled { get; }
    ValueTask<Vector> GetEmbeddingAsync(string text);
    ValueTask<Vector> GetEmbeddingAsync(CatalogItem item);
}
