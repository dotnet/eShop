using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace eShop.ServiceDefaults;

public static partial class Extensions
{
    private const string ConsoleOutputTemplate =
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static IHostApplicationBuilder AddSerilogLogging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .ReadFrom.Configuration(builder.Configuration);

            if (ShouldWriteToConsole(builder))
            {
                loggerConfiguration.WriteTo.Console(outputTemplate: ConsoleOutputTemplate);
            }

            if (builder.Environment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Debug(outputTemplate: ConsoleOutputTemplate);
            }
        }, writeToProviders: builder.Configuration.GetValue("Logging:WriteToProviders", false));

        return builder;
    }

    private static bool ShouldWriteToConsole(IHostApplicationBuilder builder)
    {
        if (builder.Configuration.GetValue("Logging:EnableConsole", false))
        {
            return true;
        }

        var environmentName = builder.Environment.EnvironmentName;

        return builder.Environment.IsDevelopment()
            || environmentName.Equals("Build", StringComparison.OrdinalIgnoreCase)
            || environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase);
    }
}
