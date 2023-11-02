namespace eShop.Catalog.API;

public class AIOptions
{
    /// <summary>Settings related to the use of OpenAI.</summary>
    public OpenAIOptions OpenAI { get; set; } = new();
}

public class OpenAIOptions
{
    /// <summary>OpenAI API key for accessing embedding LLM.</summary>
    public string APIKey { get; set; }

    /// <summary>The name of the embedding model to use.</summary>
    public string EmbeddingName { get; set; }
}
