using Microsoft.SKEval.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text;

namespace Microsoft.SKEval;

public class BatchEval<T>
{
    public const string MeterId = "Microsoft.SKEval";

    IList<IEvaluator<int>> intEvaluators = new List<IEvaluator<int>>();

    IList<IEvaluator<bool>> boolEvaluators = new List<IEvaluator<bool>>();

    string? fileName;

    IInputProcessor<T>? inputProcessor;

    IOutputProcessor? outputProcessor;

    public string? OtlpEndpoint { get; set; } = default!;

    public BatchEval<T> WithInputProcessor(IInputProcessor<T> inputProcessor)
    {
        this.inputProcessor = inputProcessor;
        return this;
    }

    public BatchEval<T> WithOutputProcessor(IOutputProcessor outputProcessor)
    {
        this.outputProcessor = outputProcessor;
        return this;
    }

    public BatchEval<T> WithCsvOutputProcessor(string filename)
    {
        return WithOutputProcessor(new CsvOutputProcessor(filename));
    }

    public BatchEval<T> AddEvaluator(IEvaluator<int> evaluator)
    {
        intEvaluators.Add(evaluator);
        return this;
    }

    public BatchEval<T> AddEvaluator(IEvaluator<bool> evaluator)
    {
        boolEvaluators.Add(evaluator);
        return this;
    }

    public async Task<BatchEvalResults> Run()
    {
        return await ProcessUserInputFile();
    }

    public BatchEval<T> WithJsonl(string fileName)
    {
        this.fileName = fileName;
        return this;
    }

    private async Task<BatchEvalResults> ProcessUserInputFile()
    {
        var meter = new Meter(MeterId);

        const int BufferSize = 128;
        using (var fileStream = File.OpenRead(fileName!))
        using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
        {
            var results = await ProcessFileLines(streamReader, meter);
            return results;
        }
    }

    private EvalMetrics InitCounters(Meter meter)
    {
        var evalMetrics = new EvalMetrics() {
            PromptCounter = meter.CreateCounter<int>($"llmeval.prompt.counter")
        };
        
        foreach (var evaluator in intEvaluators)
        {
            var histogram = meter.CreateHistogram<int>($"llmeval.{evaluator.Id.ToLowerInvariant()}.score");
            evalMetrics.ScoreHistograms.Add(evaluator.Id, histogram);
        }

        foreach (var evaluator in boolEvaluators)
        {
            evalMetrics.BooleanCounters.Add(
                $"llmeval.{evaluator.Id.ToLowerInvariant()}.failure", 
                meter.CreateCounter<int>($"{evaluator.Id.ToLowerInvariant()}.failure"));

            evalMetrics.BooleanCounters.Add(
                $"llmeval.{evaluator.Id.ToLowerInvariant()}.success", 
                meter.CreateCounter<int>($"{evaluator.Id.ToLowerInvariant()}.success"));
        }

        return evalMetrics;
    }

    private async Task<BatchEvalResults> ProcessFileLines(
       StreamReader streamReader,
       Meter meter)
    {
        var evalMetrics = InitCounters(meter);

        outputProcessor?.Init();

        var results = new BatchEvalResults();

        string? line;
        while ((line = await streamReader.ReadLineAsync()) != null)
        {
            var userInput = System.Text.Json.JsonSerializer.Deserialize<T>(line);

            var modelOutput = await inputProcessor!.Process(userInput!);

            var evalOutput = new BatchEvalPromptOutput()
            {
                Subject = modelOutput
            };

            Console.WriteLine($"Q: {modelOutput.Input}");
            Console.WriteLine($"A: {modelOutput.Output}");

            evalMetrics.PromptCounter.Add(1);

            foreach (var evaluator in intEvaluators)
            {
                var score = await evaluator.Eval(modelOutput);

                Console.WriteLine($"EVAL: {evaluator.Id.ToLowerInvariant()} SCORE: {score}");
                
                evalMetrics.ScoreHistograms[evaluator.Id.ToLowerInvariant()].Record(score);
                evalOutput.Results.Add(evaluator.Id.ToLowerInvariant(), score);
            }

            foreach (var evaluator in boolEvaluators)
            {
                var evalResult = await evaluator.Eval(modelOutput);

                Console.WriteLine($"EVAL: {evaluator.Id.ToLowerInvariant()} RESULT: {evalResult}");

                evalOutput.Results.Add(evaluator.Id.ToLowerInvariant(), evalResult);

                if (evalResult) {
                    evalMetrics.BooleanCounters[$"{evaluator.Id.ToLowerInvariant()}.success"].Add(1);
                } else {
                    evalMetrics.BooleanCounters[$"{evaluator.Id.ToLowerInvariant()}.failure"].Add(1);
                }
            }

            outputProcessor?.Process(evalOutput);
            
            results.EvalResults.Add(evalOutput);
        }

        return results;
    }

    public void ConfigureMeterBuilder()
    {
        var builder = Sdk.CreateMeterProviderBuilder()
            .AddMeter(MeterId);

        builder.AddLLMEvalMetrics(intEvaluators);

        if (string.IsNullOrEmpty(OtlpEndpoint))
        {
            builder.AddConsoleExporter();
        } else {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(OtlpEndpoint);
            });
        }

        builder.AddMeter("Microsoft.SemanticKernel*");
    
        builder.Build();
    }
}