using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Vision.Common;
using Azure.AI.Vision.ImageAnalysis;
using Pgvector;

namespace eShop.Catalog.API.Services;

public sealed class CatalogAI : ICatalogAI
{
    /// <summary>API key for accessing chat and embedding LLM.</summary>
    private readonly string _aiKey;
    /// <summary>Endpoint used for accessing chat and embedding LLM. If it's non-null, it's using Azure OpenAI; if it's null but <see cref="_aiKey"/> is non-null, it's using OpenAI.</summary>
    private readonly string _aiEndpoint;
    /// <summary>The name of the embedding model or deployment to use.</summary>
    private readonly string _aiEmbeddingModel;
    /// <summary>Optional API key for accessing <see cref="_aiVisionEndpoint"/>.</summary>
    private readonly string _aiVisionKey;
    /// <summary>Optional endpoint for accessing Azure vision service, used to generate a description of an image.</summary>
    private readonly string _aiVisionEndpoint;

    /// <summary>The web host environment.</summary>
    private readonly IWebHostEnvironment _environment;
    /// <summary>Logger for use in AI operations.</summary>
    private readonly ILogger _logger;

    public CatalogAI(IOptions<AIOptions> options, IWebHostEnvironment environment, ILogger<CatalogAI> logger)
    {
        var aiOptions = options.Value;

        // Required
        _aiKey = aiOptions.OpenAI.APIKey;
        IsEnabled = !string.IsNullOrWhiteSpace(_aiKey);

        // Required to use Azure OpenAI
        _aiEndpoint = aiOptions.OpenAI.Endpoint;

        // Required but have defaults
        _aiEmbeddingModel = aiOptions.OpenAI.EmbeddingName ?? "text-embedding-ada-002";

        // Optional
        _aiVisionKey = aiOptions.Vision.APIKey;
        _aiVisionEndpoint = aiOptions.Vision.Endpoint;

        _environment = environment;
        _logger = logger;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("API Key: {configured}", string.IsNullOrWhiteSpace(_aiKey) ? "Not configured" : "Configured");
            _logger.LogInformation("Endpoint: {configured}", string.IsNullOrWhiteSpace(_aiEndpoint) ? "Not configured" : "Configured");
            _logger.LogInformation("Embedding model: \"{model}\"", _aiEmbeddingModel);
            _logger.LogInformation("Vision API Key: {configured}", string.IsNullOrWhiteSpace(_aiVisionKey) ? "Not configured" : "Configured");
            _logger.LogInformation("Vision Endpoint: {configured}", string.IsNullOrWhiteSpace(_aiVisionKey) ? "Not configured" : "Configured");
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

        EmbeddingsOptions options = new(text);
        var result = (await GetAIClient().GetEmbeddingsAsync(_aiEmbeddingModel, options)).Value.Data[0].Embedding;
        return new Vector(result as float[] ?? [.. result]);
    }

    /// <summary>Gets an embedding vector for the specified catalog item.</summary>
    public async ValueTask<Vector> GetEmbeddingAsync(CatalogItem item)
    {
        if (!IsEnabled)
        {
            return null;
        }

        string imageDescription = await GetDescriptionFromImageAsync(CatalogApi.GetFullPath(_environment.ContentRootPath, item.PictureFileName));

        string text = string.IsNullOrWhiteSpace(imageDescription) ?
            $"{item.Name} {item.Description}" :
            $"{item.Name} {item.Description} {imageDescription}";

        return await GetEmbeddingAsync(text);
    }

    /// <summary>Gets the AI client used for creating embeddings.</summary>
    private OpenAIClient GetAIClient() =>
        // At a minimum, an API key needs to be configured.
        // If there's no separate endpoint, this is an OpenAI client.
        // If there is a separate endpoint, it's an Azure OpenAI endpoint.
        string.IsNullOrWhiteSpace(_aiKey) ? throw new InvalidOperationException("AI API key not configured") :
        string.IsNullOrWhiteSpace(_aiEndpoint) ? new OpenAIClient(_aiKey) :
        new OpenAIClient(new Uri(_aiEndpoint), new AzureKeyCredential(_aiKey));

    /// <summary>Computes a description of an image at the specified path.</summary>
    private async ValueTask<string> GetDescriptionFromImageAsync(string path)
    {
        string description = null;
        if (!string.IsNullOrWhiteSpace(_aiVisionEndpoint))
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Getting image description for \"{path}\"", path);
            }

            try
            {
                // Load the image file.
                using VisionSource imageSource = VisionSource.FromFile(path);

                // Create the analyzer to extract a caption from the image.
                using ImageAnalyzer analyzer = new(
                    new(_aiVisionEndpoint, new AzureKeyCredential(_aiVisionKey)),
                    imageSource,
                    new() { Features = ImageAnalysisFeature.Caption | ImageAnalysisFeature.Text, Language = "en", GenderNeutralCaption = true });

                // Perform the analysis. If we were able to get a caption, concatenate it into a single string.
                ImageAnalysisResult result = await analyzer.AnalyzeAsync();
                if (result.Reason == ImageAnalysisResultReason.Analyzed)
                {
                    description = result.Caption?.Content;
                    if (result.Text != null)
                    {
                        description += " " + string.Join(", ", result.Text.Lines.Select(line => line.Content));
                    }
                }
            }
            catch (Exception e)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(e, "Error getting image description for \"{path}\"", path);
                }
            }
        }

        return description;
    }
}
