using Microsoft.SemanticKernel;

namespace Microsoft.SKEval;

public class GroundednessEval : PromptScoreEval
{
    public GroundednessEval(Kernel kernel) : base("groundedness", kernel, "_prompts.groundedness.skprompt.txt") {}
}