using System.Text;

namespace Microsoft.SKEval;

public class CsvOutputProcessor(string filePath) : IOutputProcessor
{
    public string FilePath { get; set; } = filePath;

    public Task Init()
    {
        return Task.CompletedTask;
    }

    public async Task Process(BatchEvalPromptOutput evalOutput)
    {
        var output = ToCsv(evalOutput);
        
        await File.AppendAllTextAsync(FilePath, output);
    }

    private static string ToCsv(BatchEvalPromptOutput result)
    {
        var output = new StringBuilder();

        output.Append($"\"{EscapeCsvValue(result.Subject.Input)}\",");
        output.Append($"\"{EscapeCsvValue(result.Subject.Output)}\",");

        foreach (var value in result.Results.Values)
        {
            output.Append($"\"{EscapeCsvValue(value?.ToString() ?? string.Empty)}\",");
        }
        output.Length--; // Remove the trailing comma
        output.AppendLine();
        
        return output.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
        // If value contains double quotes, escape them by doubling them
        if (value.Contains('\"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        // If value contains comma, surround it with double quotes
        if (value.Contains(','))
        {
            value = $"\"{value}\"";
        }

        return value;
    }
}