#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
using System.CommandLine.Parsing;
using System.CommandLine;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SKEval;
using AI.BatchEvals;

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
    Description = "Format of the output. Options: csv, tsv, yaml, json.",
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

    var batchEval = new BatchEval<ModelInputQA>();

    batchEval.ConfigureMeterBuilder();

    batchEval
        .AddEvaluator(new CoherenceEval(kernel))
        .AddEvaluator(eShop.WebApp.AIBatchEvals.RelevanceEval.GetInstance(kernel))
        .AddEvaluator(new GroundednessEval(kernel));
    
    batchEval.WithCsvOutputProcessor("results_adversary.csv");

    var results = await batchEval
        .WithInputProcessor(chatInputProcessor)
        .WithJsonl(dataFilePath)
        .Run();

}, inputOption, outputOption);

rootCommand.AddCommand(createPostCommand);

var result = await rootCommand.InvokeAsync(args);

return result;
static void ConfigureServices(ServiceCollection serviceCollection, string[] args)
{
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
    {
        serviceCollection
            .AddOpenTelemetry()
            .WithMetrics(m =>
            {
                m.AddMeter("Microsoft.SemanticKernel*");
                m.AddMeter("Microsoft.SKEval*");
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
