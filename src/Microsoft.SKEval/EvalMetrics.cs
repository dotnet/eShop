using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;

namespace Microsoft.SKEval;

public class EvalMetrics
{
    public Counter<int> PromptCounter { get; set; } = default!;

    public IDictionary<string, Histogram<int>> ScoreHistograms { get; set; } = new Dictionary<string, Histogram<int>>();

    public IDictionary<string, Counter<int>> BooleanCounters { get; set; } = new Dictionary<string, Counter<int>>();
}
