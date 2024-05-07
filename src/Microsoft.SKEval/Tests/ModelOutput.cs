using Microsoft.SemanticKernel;
using Xunit;

namespace Microsoft.SKEval.Tests;

public class ModelOutput : SKEval.ModelOutput
{
    private Kernel? _kernel;
    
    public ModelOutput With(Kernel kernel)
    {
        this._kernel = kernel;
        return this;
    }

    public ModelOutput ShouldBeCoherent(int min = 3)
    {
        var score = new CoherenceEval(this._kernel!).Eval(this).Result;
        Assert.True(score >= min, $"Coherence of {this.Input} - score {score}, expecting min {min}.");
        return this;
    }

    public ModelOutput ShouldBeRelevant(int min = 3)
    {
        var score = new RelevanceEval(this._kernel!).Eval(this).Result;
        Assert.True(score >= min, $"Relevance of {this.Input} - score {score}, expecting min {min}.");
        return this;
    }

    public ModelOutput ShouldBeGrounded(int min = 3)
    {
        var score = new GroundednessEval(this._kernel!).Eval(this).Result;
        Assert.True(score >= min, $"Groundedness of {this.Input} - score {score}, expecting min {min}.");
        return this;
    }

    public async Task<T> Eval<T>(IEvaluator<T> eval)
    {
        return await eval.Eval(this);
    }
}