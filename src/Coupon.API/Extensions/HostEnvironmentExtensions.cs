using System.Reflection;

namespace Microsoft.Extensions.Hosting;

internal static class HostEnvironmentExtensions
{
    public static bool IsBuild(this IHostEnvironment hostEnvironment)
    {
        // Check if the environment is "Build" or the entry assembly is "GetDocument.Insider"
        // to account for scenarios where app is launching via OpenAPI build-time generation
        // via the GetDocument.Insider tool.
        return hostEnvironment.IsEnvironment("Build") || Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
    }
}
