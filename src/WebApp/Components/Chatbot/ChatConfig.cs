using System.Diagnostics.CodeAnalysis;

namespace eShop.WebApp.Chatbot;

public class ChatConfig
{
    public string ChatModel { get; }
    public string ApiKey { get; }
    public string? Endpoint { get; }

    private ChatConfig(string chatModel, string apiKey, string? endpoint)
    {
        ChatModel = chatModel;
        ApiKey = apiKey;
        Endpoint = endpoint;
    }

    public static bool TryReadFromConfig(IConfiguration configuration, [NotNullWhen(true)] out ChatConfig? result)
    {
        var chatModel = configuration["AI:OpenAI:ChatName"] ?? "gpt-35-turbo-16k";
        var apiKey = configuration["AI:OpenAI:APIKey"];
        var endpoint = configuration["AI:OpenAI:Endpoint"];
        result = !string.IsNullOrWhiteSpace(apiKey)
            ? new ChatConfig(chatModel, apiKey, endpoint)
            : null;
        return result is not null;
    }
}
