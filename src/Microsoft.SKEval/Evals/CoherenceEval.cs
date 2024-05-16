using Microsoft.SemanticKernel;

namespace Microsoft.SKEval;

public class CoherenceEval : PromptScoreEval
{
    public CoherenceEval(Kernel kernel) : base("coherence", kernel, "_prompts.coherence.skprompt.txt") {}
}
