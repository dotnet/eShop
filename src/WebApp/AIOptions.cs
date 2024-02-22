namespace eShop.WebApp;

public class AIOptions
{
    /// <summary>Settings related to the use of OpenAI.</summary>
    public OpenAIOptions OpenAI { get; set; } = new();
}

public class OpenAIOptions
{
    /// <summary>The name of the chat model to use.</summary>
    /// <remarks>When using Azure OpenAI, this should be the "Deployment name" of the chat model.</remarks>
    public string ChatModel { get; set; } = "gpt-3.5-turbo-16k";
}
