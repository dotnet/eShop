namespace Microsoft.SKEval;

public class BatchEvalPromptOutput
{
    public ModelOutput Subject { get; set; } = default!;

    public IDictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
}