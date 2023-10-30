using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using Microsoft.SemanticKernel.Orchestration;
using System.Text.Json;

// TODO: This whole file a temporary workaround until https://github.com/microsoft/semantic-kernel/issues/3017 and
// https://github.com/microsoft/semantic-kernel/issues/2932 are addressed. Once they are, we can not only
// delete this entirely, we can also simplify the logic around kernel construction, as we'll no longer need direct
// access to the OpenAIClient nor the chat model.

namespace Microsoft.SemanticKernel;

internal static class ChatCompletionExtensions
{
    public static async Task<ChatMessageBase> GetChatCompletionsWithFunctionCallingAsync(
        this IKernel kernel,
        OpenAIClient client,
        ChatHistory chat,
        string model,
        CancellationToken cancellationToken = default)
    {
        var chatCompletionOptions = new ChatCompletionsOptions(chat.Select(m => new ChatMessage(new ChatRole(m.Role.Label), m.Content)));
        chatCompletionOptions.Functions = kernel.Functions.GetFunctionViews().Select(functionView => functionView.ToOpenAIFunction().ToFunctionDefinition()).ToList();

        while (true)
        {
            ChatChoice choice;
            while (true)
            {
                var response = await client.GetChatCompletionsAsync(model, chatCompletionOptions);
                choice = response.Value.Choices[0];

                if (choice.FinishReason != CompletionsFinishReason.FunctionCall)
                {
                    return new SKChatMessage(AuthorRole.Assistant, choice.Message.Content);
                }

                OpenAIFunctionResponse functionCall = FromFunctionCall(choice.Message.FunctionCall);
                if (!kernel.Functions.TryGetFunction(functionCall.PluginName, functionCall.FunctionName, out ISKFunction? function))
                {
                    return new SKChatMessage(AuthorRole.Assistant, choice.Message.Content);
                }

                ContextVariables context = new();
                foreach (KeyValuePair<string, object> parameter in functionCall.Parameters)
                {
                    context.Set(parameter.Key, parameter.Value.ToString());
                }

                KernelResult functionResult = await kernel.RunAsync(function, context, cancellationToken).ConfigureAwait(false);
                chatCompletionOptions.Messages.Add(new ChatMessage(ChatRole.Function, functionResult.GetValue<string>() ?? "") { Name = choice.Message.FunctionCall.Name });
            }
        }

        static OpenAIFunctionResponse FromFunctionCall(FunctionCall functionCall)
        {
            OpenAIFunctionResponse response = new();

            int pos = functionCall.Name.IndexOf(OpenAIFunction.NameSeparator, StringComparison.Ordinal);
            if (pos < 0)
            {
                response.FunctionName = functionCall.Name;
            }
            else
            {
                response.PluginName = functionCall.Name.AsSpan(0, pos).Trim().ToString();
                response.FunctionName = functionCall.Name.AsSpan(pos + OpenAIFunction.NameSeparator.Length).Trim().ToString();
            }

            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(functionCall.Arguments);
            if (parameters is not null)
            {
                response.Parameters = parameters;
            }

            return response;
        }
    }

    private sealed class SKChatMessage(AuthorRole role, string content) : ChatMessageBase(role, content) { }
}
