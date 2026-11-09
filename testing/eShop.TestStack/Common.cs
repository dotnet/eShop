using System.Text.Json;
using System.Text.Json.Nodes;

namespace eShop.TestStack;

/// <summary>Process exit codes. Stable so agents can branch on them.</summary>
internal static class ExitCodes
{
    public const int Ok = 0;
    public const int Error = 1;
    public const int NotReady = 2;
    public const int InvalidScenario = 3;
    public const int Usage = 64;
}

internal sealed class TestStackException(string message, int exitCode = ExitCodes.Error, Exception? inner = null)
    : Exception(message, inner)
{
    public int ExitCode { get; } = exitCode;
}

/// <summary>
/// Progress goes to stderr; exactly one JSON result document goes to stdout so callers can parse it.
/// </summary>
internal static class Output
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static void Log(string message) => Console.Error.WriteLine($"[teststack] {message}");

    public static void Result(JsonObject result) => Console.Out.WriteLine(result.ToJsonString(JsonOptions));
}

internal sealed class ParsedArgs
{
    public string Command { get; private init; } = "";
    public List<string> Positionals { get; } = [];
    private readonly Dictionary<string, string?> _options = new(StringComparer.Ordinal);

    public bool Flag(string name) => _options.ContainsKey(name);

    public string? Option(string name) => _options.TryGetValue(name, out var value) ? value : null;

    public int IntOption(string name, int defaultValue)
    {
        var raw = Option(name);
        if (raw is null)
        {
            return defaultValue;
        }

        return int.TryParse(raw, out var value)
            ? value
            : throw new TestStackException($"--{name} expects an integer, got '{raw}'.", ExitCodes.Usage);
    }

    private static readonly HashSet<string> ValueOptions = ["timeout", "root", "config"];

    public static ParsedArgs Parse(string[] args)
    {
        var parsed = new ParsedArgs { Command = args.Length > 0 ? args[0] : "" };
        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var name = arg[2..];
                string? value = null;
                var eq = name.IndexOf('=');
                if (eq >= 0)
                {
                    value = name[(eq + 1)..];
                    name = name[..eq];
                }
                else if (ValueOptions.Contains(name))
                {
                    value = i + 1 < args.Length
                        ? args[++i]
                        : throw new TestStackException($"--{name} requires a value.", ExitCodes.Usage);
                }

                parsed._options[name] = value;
            }
            else
            {
                parsed.Positionals.Add(arg);
            }
        }

        return parsed;
    }
}
