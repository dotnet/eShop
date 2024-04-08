using Microsoft.SemanticKernel.Memory;
using Pgvector;

namespace eShop.Catalog.API.Services;

public interface ICatalogAI
{
    bool IsEnabled { get; }
    ValueTask<Vector> GetEmbeddingAsync(string text);
    ValueTask<string> SaveToMemoryAsync(CatalogItem item);
    IAsyncEnumerable<MemoryQueryResult> SearchMemoryAsync(string query, int pageSize);
}
