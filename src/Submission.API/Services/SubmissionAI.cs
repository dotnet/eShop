using System.Diagnostics;
using Microsoft.Extensions.AI;
using Pgvector;

namespace Inked.Submission.API.Services;

public sealed class SubmissionAI : ISubmissionAI
{
    private const int EmbeddingDimensions = 384;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    /// <summary>The web host environment.</summary>
    private readonly IWebHostEnvironment _environment;

    /// <summary>Logger for use in AI operations.</summary>
    private readonly ILogger _logger;

    public SubmissionAI(IWebHostEnvironment environment, ILogger<SubmissionAI> logger,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = null)
    {
        _embeddingGenerator = embeddingGenerator;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsEnabled => _embeddingGenerator is not null;

    /// <inheritdoc />
    public ValueTask<Vector> GetEmbeddingAsync(Model.Submission item)
    {
        return IsEnabled ? GetEmbeddingAsync(CatalogItemToString(item)) : ValueTask.FromResult<Vector>(null);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<Vector>> GetEmbeddingsAsync(IEnumerable<Model.Submission> items)
    {
        if (IsEnabled)
        {
            var timestamp = Stopwatch.GetTimestamp();

            var embeddings = await _embeddingGenerator.GenerateAsync(items.Select(CatalogItemToString));
            var results = embeddings.Select(m => new Vector(m.Vector[..EmbeddingDimensions])).ToList();

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Generated {EmbeddingsCount} embeddings in {ElapsedMilliseconds}s", results.Count,
                    Stopwatch.GetElapsedTime(timestamp).TotalSeconds);
            }

            return results;
        }

        return null;
    }

    /// <inheritdoc />
    public async ValueTask<Vector> GetEmbeddingAsync(string text)
    {
        if (IsEnabled)
        {
            var timestamp = Stopwatch.GetTimestamp();

            var embedding = await _embeddingGenerator.GenerateEmbeddingVectorAsync(text);
            embedding = embedding[..EmbeddingDimensions];

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Generated embedding in {ElapsedMilliseconds}s: '{Text}'",
                    Stopwatch.GetElapsedTime(timestamp).TotalSeconds, text);
            }

            return new Vector(embedding);
        }

        return null;
    }

    private static string CatalogItemToString(Model.Submission item)
    {
        return $"{item.Title} {item.Description}";
    }
}
