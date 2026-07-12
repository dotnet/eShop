using Microsoft.Extensions.Logging;

namespace eShop.Testing.Common;

public sealed class TestOutputLoggerProvider : ILoggerProvider
{
    public static TestOutputLoggerProvider Instance { get; } = new();

    public ILogger CreateLogger(string categoryName) => new TestOutputLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class TestOutputLogger(string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            TestOutputWriter.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff} {logLevel}] {categoryName}: {message}");

            if (exception is not null)
            {
                TestOutputWriter.WriteLine(exception.ToString());
            }
        }
    }
}
