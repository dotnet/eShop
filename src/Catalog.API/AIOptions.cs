namespace eShop.Catalog.API;

public class AIOptions
{
    public OpenAIOptions OpenAI { get; set; } = new();
    public VisionOptions Vision { get; set; } = new();
}

public class OpenAIOptions
{
    /// <summary>API key for accessing chat and embedding LLM.</summary>
    public string APIKey { get; set; }

    /// <summary>Endpoint used for accessing chat and embedding LLM. If it's non-null, it's using Azure OpenAI; if it's null but <see cref="APIKey"/> is non-null, it's using OpenAI.</summary>
    public string Endpoint { get; set; }

    /// <summary>The name of the embedding model or deployment to use.</summary>
    public string EmbeddingName { get; set; }
}

public class VisionOptions
{
    /// <summary>Optional API key for accessing <see cref="Endpoint"/>.</summary>
    public string APIKey { get; set; }

    /// <summary>Optional endpoint for accessing Azure vision service, used to generate a description of an image.</summary>
    public string Endpoint { get; set; }
}
