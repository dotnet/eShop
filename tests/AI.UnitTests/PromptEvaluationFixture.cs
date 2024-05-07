#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            if (config["ESHOP_TESTS_SK_KERNEL_COMPLETION_TYPE"]?.ToString().Equals("openai") ?? false)
            {
                builder.AddOpenAIChatCompletion(
                   modelId: config["ESHOP_OPENAI_MODEL"],
                   endpoint: new Uri(config["ESHOP_OPENAI_ENDPOINT"]),
                   apiKey: config["ESHOP_OPENAI_KEY"]);
            }
            else
            {
                builder.AddAzureOpenAIChatCompletion(
                    config["AZURE_OPENAI_MODEL"],
                    config["AZURE_OPENAI_ENDPOINT"],
                    config["AZURE_OPENAI_KEY"]);
            }
            
            Kernel = builder.Build();
        }
    }
}
