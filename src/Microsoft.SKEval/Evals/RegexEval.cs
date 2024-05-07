using Microsoft.SemanticKernel;

namespace Microsoft.SKEval;

public class RegexEval : IEvaluator<bool>
{
    private readonly string pattern;

    public string Id { get; }

    public RegexEval(string id, string pattern)
    {
        this.Id = id;
        this.pattern = pattern;
    }

    public Task<bool> Eval(ModelOutput modelOutput)
    {
        return Task.FromResult<bool>(System.Text.RegularExpressions.Regex.IsMatch(modelOutput.Output, pattern));
    }
}