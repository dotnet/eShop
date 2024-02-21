using Azure.AI.OpenAI;
using Pgvector;

namespace eShop.Catalog.API.Services;

public sealed class CatalogAI : ICatalogAI
{
    private readonly string _aiEmbeddingModel;
    private readonly OpenAIClient _openAIClient;

    /// <summary>The web host environment.</summary>
    private readonly IWebHostEnvironment _environment;
    /// <summary>Logger for use in AI operations.</summary>
    private readonly ILogger _logger;

    public CatalogAI(IOptions<AIOptions> options, IWebHostEnvironment environment, ILogger<CatalogAI> logger, OpenAIClient openAIClient = null)
    {
        var aiOptions = options.Value;
        _openAIClient = openAIClient;
        _aiEmbeddingModel = aiOptions.OpenAI.EmbeddingName ?? "text-embedding-ada-002";
        IsEnabled = _openAIClient != null;
        _environment = environment;
        _logger = logger;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Embedding model: \"{model}\"", _aiEmbeddingModel);
        }
    }

    /// <summary>Gets whether the AI system is enabled.</summary>
    public bool IsEnabled { get; }

    /// <summary>Gets an embedding vector for the specified text.</summary>
    public async ValueTask<Vector> GetEmbeddingAsync(string text)
    {
        if (!IsEnabled)
        {
            return null;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Getting embedding for \"{text}\"", text);
        }

        EmbeddingsOptions options = new(_aiEmbeddingModel, [text]);
        return new Vector((await _openAIClient.GetEmbeddingsAsync(options)).Value.Data[0].Embedding);
    }

    /// <summary>Gets an embedding vector for the specified catalog item.</summary>
    public ValueTask<Vector> GetEmbeddingAsync(CatalogItem item) => IsEnabled ?
        GetEmbeddingAsync($"{item.Name} {item.Description}") :
        ValueTask.FromResult<Vector>(null);
}
