using Microsoft.SemanticKernel;

namespace Microsoft.SKEval;

public class RelevanceEval : PromptScoreEval
{
    public RelevanceEval(Kernel kernel) : base("relevance", kernel, "_prompts.relevance.skprompt.txt") {}
}