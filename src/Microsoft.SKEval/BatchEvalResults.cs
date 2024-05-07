using System.Text;

namespace Microsoft.SKEval;

public class BatchEvalResults
{
    public IList<BatchEvalPromptOutput> EvalResults { get; set; } = new List<BatchEvalPromptOutput>();

    public string ToCsv()
    {
        var output = new StringBuilder();

        // Headers
        if (EvalResults.Any())
        {
            output.Append("\"Input\",\"Output\",");

            var first = EvalResults.First();
            foreach (var key in first.Results.Keys)
            {
                output.Append($"\"{EscapeCsvValue(key)}\",");
            }
            output.Length--; // Remove the trailing comma
            output.AppendLine();
        }

        // Body
        foreach (var result in EvalResults)
        {
            output.Append($"\"{EscapeCsvValue(result.Subject.Input)}\",");
            output.Append($"\"{EscapeCsvValue(result.Subject.Output)}\",");

            foreach (var value in result.Results.Values)
            {
                output.Append($"\"{EscapeCsvValue(value?.ToString() ?? string.Empty)}\",");
            }
            output.Length--; // Remove the trailing comma
            output.AppendLine();
        }

        return output.ToString();
    }

    private string EscapeCsvValue(string value)
    {
        // If value contains double quotes, escape them by doubling them
        if (value.Contains("\""))
        {
            value = value.Replace("\"", "\"\"");
        }

        // If value contains comma, surround it with double quotes
        if (value.Contains(","))
        {
            value = $"\"{value}\"";
        }

        return value;
    }
}