namespace Microsoft.SKEval;

public class LenghtEval : IEvaluator<int>
{
    public string Id { get; } = "length";

    public Task<int> Eval(ModelOutput modelOutput)
    {
        return Task.FromResult<int>(modelOutput.Output.Length);
    }
}