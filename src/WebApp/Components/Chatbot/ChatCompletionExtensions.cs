using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;
using Microsoft.SemanticKernel.Orchestration;

// TODO: This file is temporary until https://github.com/microsoft/semantic-kernel/issues/2932 is addressed.

namespace Microsoft.SemanticKernel;

internal static class ChatCompletionExtensions
{
    public static async Task<IChatResult> GetChatCompletionsWithFunctionCallingAsync(
        this IKernel kernel,
        ChatHistory chat)
    {
        IChatCompletion chatCompletion = kernel.GetService<IChatCompletion>();
        OpenAIRequestSettings requestSettings = new()
        {
            Functions = kernel.Functions.GetFunctionViews().Select(f => f.ToOpenAIFunction()).ToList(),
            FunctionCall = OpenAIRequestSettings.FunctionCallAuto,
        };

        while (true)
        {
            IChatResult chatResult = (await chatCompletion.GetChatCompletionsAsync(chat, requestSettings))[0];

            OpenAIFunctionResponse? functionResponse = chatResult.GetOpenAIFunctionResponse();
            if (functionResponse is null ||
                !kernel.Functions.TryGetFunctionAndContext(functionResponse, out ISKFunction? function, out ContextVariables? context))
            {
                return chatResult;
            }

            chat.AddFunctionMessage(
                (await kernel.RunAsync(function, context)).GetValue<string>()!, 
                functionResponse.FullyQualifiedName);
        }
    }
}
