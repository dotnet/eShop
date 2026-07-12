using eShop.Catalog.API.Model;
using eShop.Catalog.API.Services;

using Pgvector;

namespace eShop.Catalog.FunctionalTests.Mocks;

internal sealed class FakeCatalogAI : ICatalogAI
{
    public bool IsEnabled => false;

    public ValueTask<Vector?> GetEmbeddingAsync(string text) => ValueTask.FromResult<Vector?>(null);

    public ValueTask<Vector?> GetEmbeddingAsync(CatalogItem item) => ValueTask.FromResult<Vector?>(null);

    public ValueTask<IReadOnlyList<Vector>?> GetEmbeddingsAsync(IEnumerable<CatalogItem> item) => ValueTask.FromResult<IReadOnlyList<Vector>?>(null);
}
