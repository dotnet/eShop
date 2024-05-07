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

            builder.AddAzureOpenAIChatCompletion(
                    config["AZURE_OPENAI_MODEL"],
                    config["AZURE_OPENAI_ENDPOINT"],
                    config["AZURE_OPENAI_KEY"]);

            Kernel = builder.Build();
        }
    }
}
