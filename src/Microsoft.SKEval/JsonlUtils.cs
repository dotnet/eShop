using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text;

namespace Microsoft.SKEval;

public class JsonlUtils
{
    public static async Task<IList<T>> Preview<T>(string fileName, int top = 10)
    {
        var result = new List<T>();

        var i = 0;
        const int BufferSize = 128;
        using (var fileStream = File.OpenRead(fileName!))
        using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
        {
            string? line;
            while ((line = await streamReader.ReadLineAsync()) != null && i++ < top)
            {
                var userInput = System.Text.Json.JsonSerializer.Deserialize<T>(line);

                result.Add(userInput!);
            }
        }
        
        return result;
    }
}