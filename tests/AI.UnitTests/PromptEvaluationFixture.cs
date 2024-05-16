#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace WebApp.UnitTests
{
    public class PromptEvaluationFixture
    {
        public Kernel Kernel { get; }

        public PromptEvaluationFixture()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            var config = configurationBuilder.Build();

            var builder = Kernel.CreateBuilder();

            var completionType = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ESHOP_TESTS_AI_COMPLETION_TYPE"));

            if (!completionType.ToString().ToLowerInvariant().Equals("openai"))
            {
                builder.AddOpenAIChatCompletion(
                   modelId: config["ESHOP_AI_MODEL"],
                   endpoint: new Uri(config["ESHOP_AI_ENDPOINT"]),
                   apiKey: config["ESHOP_AI_KEY"]);
            }
            else
            {
                builder.AddAzureOpenAIChatCompletion(
                    config["AZURE_AI_MODEL"],
                    config["AZURE_AI_ENDPOINT"],
                    config["AZURE_AI_KEY"]);
            }
            
            Kernel = builder.Build();
        }
    }
}
