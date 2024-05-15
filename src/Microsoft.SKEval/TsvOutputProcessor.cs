using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SKEval
{
    public class TsvOutputProcessor : IOutputProcessor
    {
        public TsvOutputProcessor()
        {
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        public void Process(BatchEvalPromptOutput evalOutput)
        {
            var output = ToTsv(evalOutput);

            Console.WriteLine(output);
        }

        private static string ToTsv(BatchEvalPromptOutput result)
        {
            var output = new StringBuilder();

            output.Append($"{EscapeTsvValue(result.Subject.Input)}\t");
            output.Append($"{EscapeTsvValue(result.Subject.Output)}\t");

            foreach (var value in result.Results.Values)
            {
                output.Append($"{EscapeTsvValue(value?.ToString() ?? string.Empty)}\t");
            }
            output.Length--; // Remove the trailing tab
            output.AppendLine();

            return output.ToString();
        }

        private static string EscapeTsvValue(string value)
        {
            // If value contains tabs, replace them with spaces
            if (value.Contains('\t'))
            {
                value = value.Replace("\t", " ");
            }

            return value;
        }
    }
}
