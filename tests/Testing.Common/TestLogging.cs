using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eShop.Testing.Common;

public static class TestLogging
{
    public static IReadOnlyDictionary<string, string?> DefaultConfiguration { get; } =
        new Dictionary<string, string?>
        {
            ["Logging:EnableConsole"] = "true",
            ["Logging:WriteToProviders"] = "true",
            ["Logging:LogLevel:Default"] = "Debug",
            ["Logging:LogLevel:eShop"] = "Debug",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Information",
            ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Information",
            ["Serilog:MinimumLevel:Default"] = "Debug",
            ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Information",
            ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"] = "Information",
        };

    public static IWebHostBuilder ConfigureTestLogging(this IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(TestOutputLoggerProvider.Instance);
            logging.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "HH:mm:ss.fff ";
                options.IncludeScopes = true;
                options.SingleLine = false;
            });
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Microsoft", LogLevel.Information);
            logging.AddFilter("System", LogLevel.Information);
            logging.AddFilter("eShop", LogLevel.Debug);
        });

        return builder;
    }

    public static HttpClient CreateLoggedClient<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory, DelegatingHandler innerHandler)
        where TEntryPoint : class
        => factory.CreateDefaultClient(innerHandler, new HttpTrafficLoggingHandler());

    public static void ConfigureMSTestOutput(TestContext testContext)
    {
        TestLogCapture.Clear();
        TestOutputWriter.SetWriter(message => testContext.WriteLine(message));
    }

    public static ILoggerFactory CreateLoggerFactory(string categoryPrefix = "eShop")
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddProvider(TestOutputLoggerProvider.Instance);
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddFilter(categoryPrefix, LogLevel.Debug);
        });
    }

    public static ILogger<T> CreateLogger<T>(ILoggerFactory? loggerFactory = null) =>
        (loggerFactory ?? CreateLoggerFactory()).CreateLogger<T>();

    public static ILogger CreateLogger(string category, ILoggerFactory? loggerFactory = null) =>
        (loggerFactory ?? CreateLoggerFactory()).CreateLogger(category);

    public static void LogTestBoundary(ILogger logger, string testName, string phase)
    {
        logger.LogInformation("===== TEST {Phase}: {TestName} =====", phase, testName);
    }

    public static void FlushToTestOutput()
    {
        TestOutputWriter.FlushCapturedMessages();
    }

    public static void ClearCapturedLogs() => TestLogCapture.Clear();
}
