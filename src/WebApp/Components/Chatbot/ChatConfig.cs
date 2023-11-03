using System.Diagnostics.CodeAnalysis;

namespace eShop.WebApp.Chatbot;

public record ChatConfig(string ApiKey, string ChatModel)
{
    public static bool TryReadFromConfig(IConfiguration configuration, [NotNullWhen(true)] out ChatConfig? result)
    {
        var apiKey = configuration["AI:OpenAI:APIKey"];
        var chatModel = configuration["AI:OpenAI:ChatName"] ?? "gpt-3.5-turbo-16k";
        result = !string.IsNullOrWhiteSpace(apiKey) ? new ChatConfig(apiKey, chatModel) : null;
        return result is not null;
    }
}
