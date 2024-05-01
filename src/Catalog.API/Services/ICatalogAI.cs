using Pgvector;

namespace eShop.Catalog.API.Services;

public interface ICatalogAI
{
    /// <summary>Gets whether the AI system is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Gets an embedding vector for the specified text.</summary>
    ValueTask<Vector> GetEmbeddingAsync(string text);
    
    /// <summary>Gets an embedding vector for the specified catalog item.</summary>
    ValueTask<Vector> GetEmbeddingAsync(CatalogItem item);

    /// <summary>Gets embedding vectors for the specified catalog items.</summary>
    ValueTask<IReadOnlyList<Vector>> GetEmbeddingsAsync(IEnumerable<CatalogItem> item);
}
