using System.Diagnostics;
using System.Reflection;

namespace eShop.Testing.Common;

public static class TestOutputWriter
{
    private static readonly AsyncLocal<Action<string>?> CurrentWriter = new();

    public static void SetWriter(Action<string>? writer) => CurrentWriter.Value = writer;

    public static void WriteLine(string message)
    {
        TestLogCapture.Add(message);

        if (TryWriteWithCurrentWriter(message))
        {
            return;
        }

        if (TryWriteWithXunitTestOutput(message))
        {
            return;
        }

        Debug.WriteLine(message);
    }

    public static void FlushCapturedMessages()
    {
        foreach (var message in TestLogCapture.Drain())
        {
            if (!TryWriteWithCurrentWriter(message))
            {
                TryWriteWithXunitTestOutput(message);
            }
        }
    }

    private static bool TryWriteWithCurrentWriter(string message)
    {
        var writer = CurrentWriter.Value;
        if (writer is null)
        {
            return false;
        }

        writer(message);
        return true;
    }

    private static bool TryWriteWithXunitTestOutput(string message)
    {
        foreach (var assemblyName in new[] { "xunit.v3.core.mtp-v2", "xunit.v3.core", "xunit.core" })
        {
            var testContextType = Type.GetType($"Xunit.TestContext, {assemblyName}");
            if (testContextType is null)
            {
                continue;
            }

            var current = testContextType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (current is null)
            {
                continue;
            }

            var outputHelper = current.GetType().GetProperty("TestOutputHelper")?.GetValue(current);
            if (outputHelper is null)
            {
                continue;
            }

            var writeLine = outputHelper.GetType().GetMethod("WriteLine", [typeof(string)]);
            if (writeLine is null)
            {
                continue;
            }

            writeLine.Invoke(outputHelper, [message]);
            return true;
        }

        return false;
    }
}
