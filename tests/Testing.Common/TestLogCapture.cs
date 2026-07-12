using System.Collections.Concurrent;

namespace eShop.Testing.Common;

public static class TestLogCapture
{
    private static readonly ConcurrentQueue<string> Messages = new();

    public static void Add(string message) => Messages.Enqueue(message);

    public static IReadOnlyList<string> Drain()
    {
        var drained = new List<string>();

        while (Messages.TryDequeue(out var message))
        {
            drained.Add(message);
        }

        return drained;
    }

    public static void Clear()
    {
        while (Messages.TryDequeue(out _))
        {
        }
    }
}
