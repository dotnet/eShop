using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.SemanticKernel.Connectors;
using Microsoft.Extensions.Logging;

namespace Microsoft.SKEval;

public class PromptScoreEval : IEvaluator<int>
{
    private readonly Kernel kernel;

    private readonly KernelFunction function;

    public ILogger Logger { get; set; } = default!;

    public string Id { get; }

    public PromptScoreEval(string id, Kernel kernel, KernelFunction function)
    {
        this.function = function;
        this.kernel = kernel;
        this.Id = id;
    }

    public PromptScoreEval(string id, Kernel kernel, string embeddedPrompt)
    {
        this.kernel = kernel;
        this.Id = id;
        
        string promptTemplate = EmbeddedResource.Read(embeddedPrompt)!;
        
        this.function = kernel.CreateFunctionFromPrompt(promptTemplate);
    }

    public async Task<int> Eval(ModelOutput modelOutput)
    {
        var promptArgs = new KernelArguments
            {
                { "Question", modelOutput.Input },
                { "Answer", modelOutput.Output }
            };

        var evalResult = await function.InvokeAsync(kernel, promptArgs);

        Logger?.LogDebug($"Model Eval Answer: {evalResult}");

        var evalInt = 0;

        try
        {
            evalInt = int.Parse(evalResult.ToString().Trim());
        } catch (Exception ex) {
            Logger?.LogError(ex.Message);
        }

        return evalInt;
    }
}
