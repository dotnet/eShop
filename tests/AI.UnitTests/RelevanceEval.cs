using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SKEval;

namespace eShop.WebApp.UnitTests;

internal class RelevanceEval
{
    public static PromptScoreEval GetInstance(Kernel kernel)
    {
        var functions = kernel.CreatePluginFromPromptDirectory("_prompts");
        return new PromptScoreEval("relevance", kernel, functions["relevance"]);
    }
}
