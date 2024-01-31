using Azure;
using Azure.AI.OpenAI;
using Pgvector;

namespace eShop.Catalog.API.Services;

public sealed class CatalogAI : ICatalogAI
{
    /// <summary>OpenAI API key for accessing embedding LLM.</summary>
    private readonly string _aiKey;
    /// <summary>Optional OpenAI API endpoint.</summary>
    private readonly string _aiEndpoint;
    /// <summary>The name of the embedding model to use.</summary>
    private readonly string _aiEmbeddingModel;

    /// <summary>The web host environment.</summary>
    private readonly IWebHostEnvironment _environment;
    /// <summary>Logger for use in AI operations.</summary>
    private readonly ILogger _logger;

    public CatalogAI(IOptions<AIOptions> options, IWebHostEnvironment environment, ILogger<CatalogAI> logger)
    {
        var aiOptions = options.Value;

        _aiKey = aiOptions.OpenAI.ApiKey;
        _aiEndpoint = aiOptions.OpenAI.Endpoint;
        _aiEmbeddingModel = aiOptions.OpenAI.EmbeddingName ?? "text-embedding-ada-002";
        IsEnabled = !string.IsNullOrWhiteSpace(_aiKey);

        _environment = environment;
        _logger = logger;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("API Key: {configured}", string.IsNullOrWhiteSpace(_aiKey) ? "Not configured" : "Configured");
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
        return new Vector((await GetAIClient().GetEmbeddingsAsync(options)).Value.Data[0].Embedding);
    }

    /// <summary>Gets an embedding vector for the specified catalog item.</summary>
    public ValueTask<Vector> GetEmbeddingAsync(CatalogItem item) => IsEnabled ?
        GetEmbeddingAsync($"{item.Name} {item.Description}") :
        ValueTask.FromResult<Vector>(null);

    /// <summary>Gets the AI client used for creating embeddings.</summary>
    private OpenAIClient GetAIClient() => !string.IsNullOrWhiteSpace(_aiKey) ?
        !string.IsNullOrWhiteSpace(_aiEndpoint) ?
            new OpenAIClient(new Uri(_aiEndpoint), new AzureKeyCredential(_aiKey)) :
            new OpenAIClient(_aiKey) :
        throw new InvalidOperationException("AI API key not configured");
}
