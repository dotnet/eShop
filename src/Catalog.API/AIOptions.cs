namespace eShop.Catalog.API;

public class AIOptions
{
    /// <summary>Settings related to the use of OpenAI.</summary>
    public OpenAIOptions OpenAI { get; set; } = new();
}

public class OpenAIOptions
{
    /// <summary>OpenAI API key for accessing embedding LLM.</summary>
    public string ApiKey { get; set; }

    /// <summary>Optional endpoint for which OpenAI API to access.</summary>
    public string Endpoint { get; set; }

    /// <summary>The name of the embedding model to use.</summary>
    /// <remarks>When using Azure OpenAI, this should be the "Deployment name" of the embedding model.</remarks>
    public string EmbeddingName { get; set; }
}
