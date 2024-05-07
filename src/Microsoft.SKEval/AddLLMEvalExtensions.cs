using Microsoft.SKEval;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text;

namespace Microsoft.SKEval.Metrics;

public static class AddLLMEvalExtensions
{

    public static MeterProviderBuilder AddLLMEvalMetrics(
        this MeterProviderBuilder builder,
        IList<IEvaluator<int>> intEvaluators)
    {
        foreach (var evaluator in intEvaluators)
        {
            builder.AddView(
                instrumentName: $"{evaluator.Id.ToLowerInvariant()}.score",
                new ExplicitBucketHistogramConfiguration { Boundaries = [1, 2, 3, 4, 5] });
        }

        return builder;
    }
}