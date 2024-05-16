#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
using System.CommandLine.Parsing;
using System.CommandLine;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SKEval;
using Microsoft.ApplicationInsights.Extensibility;
using Azure.Monitor.OpenTelemetry.Exporter;
using AI.BatchEvals;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var serviceCollection = new ServiceCollection();

ConfigureServices(serviceCollection, args);

using var serviceProvider = serviceCollection.BuildServiceProvider();

var debugOption = new Option<bool>("--debug")
{
    Description = "Enable debug logging"
};

var inputOption = new Option<string>("--input")
{
    Description = "Input data jsonl file to process",
    IsRequired = true
};

var outputOption = new Option<string>("--format")
{
    Description = "Format of the output. Options: csv, tsv, json.",
    IsRequired = true
};

var rootCommand = new RootCommand();

rootCommand.AddGlobalOption(debugOption);

var createPostCommand = new Command("run", "Run standard evaluations");

createPostCommand.AddOption(inputOption);
createPostCommand.AddOption(outputOption);

createPostCommand.SetHandler(async (dataFilePath, format) =>
{
    var kernel = serviceProvider.GetRequiredService<Kernel>();
    var chatInputProcessor = serviceProvider.GetRequiredService<ChatInputProcessor>();
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

    var batchEval = new BatchEval<ModelInputQA>(loggerFactory.CreateLogger(typeof(Program)));

    batchEval
        .AddEvaluator(new CoherenceEval(kernel))
        .AddEvaluator(eShop.WebApp.AIBatchEvals.RelevanceEval.GetInstance(kernel));

    ConfigureMetrics<ModelInputQA>(batchEval);

    switch (format.ToLowerInvariant())
    {
        case "csv":
            batchEval.WithOutputProcessor(new CsvOutputProcessor());
            break;
        case "tsv":
            batchEval.WithOutputProcessor(new TsvOutputProcessor());
            break;
        default:
        case "json":
            batchEval.WithOutputProcessor(new JsonOutputProcessor());
            break;
    }

    var results = await batchEval
        .WithInputProcessor(chatInputProcessor)
        .WithJsonl(dataFilePath)
        .Run();

}, inputOption, outputOption);

rootCommand.AddCommand(createPostCommand);

var result = await rootCommand.InvokeAsync(args);

return result;

static void ConfigureMetrics<T>(BatchEval<T> batchEval)
{
    var builder = batchEval.CreateMeterProviderBuilder();

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
    {
        builder.AddAzureMonitorMetricExporter();
    } 
    
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTLP_ENDPOINT"))) {
        builder.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTLP_ENDPOINT")!);
        });
    } else {
        builder.AddConsoleExporter();
    }

    builder.AddMeter("Microsoft.SemanticKernel*");
    
    builder.Build();
}

static void ConfigureServices(ServiceCollection serviceCollection, string[] args)
{
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
    {
        serviceCollection.AddApplicationInsightsTelemetryWorkerService((options) => 
            options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"));

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.AddAzureMonitorLogExporter();
            });
        });
    }

    serviceCollection
        .AddLogging(configure =>
        {
            configure.AddSimpleConsole(options => options.TimestampFormat = "hh:mm:ss ");

            if (args.Any("--debug".Contains))
            {
                configure.SetMinimumLevel(LogLevel.Debug);
            }
            
            configure.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("Category", LogLevel.Information);
        })
        .AddSingleton((sp) =>
        {
            var builder = Kernel.CreateBuilder();

            var completionType = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ESHOP_TESTS_AI_COMPLETION_TYPE"));

            if (!completionType.ToString().ToLowerInvariant().Equals("openai"))
            {
                builder.AddAzureOpenAIChatCompletion(
                    Environment.GetEnvironmentVariable("AZURE_AI_MODEL")!,
                    Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT")!,
                    Environment.GetEnvironmentVariable("AZURE_AI_KEY")!);
            }
            else
            {
                builder.AddOpenAIChatCompletion(
                    modelId: Environment.GetEnvironmentVariable("ESHOP_AI_MODEL")!,
                    endpoint: new Uri(Environment.GetEnvironmentVariable("ESHOP_AI_ENDPOINT")!),
                    apiKey: Environment.GetEnvironmentVariable("ESHOP_AI_KEY")!);
            }
           
            return builder.Build();
        })
        .AddSingleton((sp) =>
        {
            return new ChatInputProcessor(sp.GetRequiredService<Kernel>());
        });
}
