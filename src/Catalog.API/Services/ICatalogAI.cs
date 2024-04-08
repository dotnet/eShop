using Microsoft.SemanticKernel.Memory;

namespace eShop.Catalog.API.Services;

public interface ICatalogAI
{
    bool IsEnabled { get; }
    ValueTask<string> SaveToMemoryAsync(CatalogItem item);
    IAsyncEnumerable<MemoryQueryResult> SearchMemoryAsync(string query, int pageSize);
}
