using System.IO;
using System.Text.Json;

namespace Microsoft.SKEval
{
    public class JsonOutputProcessor : IOutputProcessor
    {
        public string FilePath { get; set; }

        public JsonOutputProcessor(string filePath)
        {
            FilePath = filePath;
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        public void Process(BatchEvalPromptOutput evalOutput)
        {
            var output = JsonSerializer.Serialize(evalOutput);

            Console.WriteLine(output);
        }
    }
}
