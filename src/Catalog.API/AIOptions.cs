namespace eShop.Catalog.API;

public class AIOptions
{
    /// <summary>Settings related to the use of OpenAI.</summary>
    public OpenAIOptions OpenAI { get; set; } = new();
}

public class OpenAIOptions
{
    /// <summary>
    /// Gets or sets whether OpenAI services should be enabled.
    /// </summary>
    public bool Enable { get; set; } = true;

    /// <summary>The name of the embedding model to use.</summary>
    /// <remarks>When using Azure OpenAI, this should be the "Deployment name" of the embedding model.</remarks>
    public string EmbeddingName { get; set; }
}
