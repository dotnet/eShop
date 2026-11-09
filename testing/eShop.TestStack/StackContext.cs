using System.Diagnostics;
using System.Text.Json;
using Npgsql;

namespace eShop.TestStack;

/// <summary>Resolved paths, credentials and connection factories for a running test stack.</summary>
internal sealed class StackContext
{
    public string Root { get; }
    public StackConfig Config { get; }
    private readonly Dictionary<string, string> _env;

    private StackContext(string root, StackConfig config, Dictionary<string, string> env)
    {
        Root = root;
        Config = config;
        _env = env;
    }

    public static StackContext Create(ParsedArgs args, bool requireEnvFile = true)
    {
        var root = Path.GetFullPath(args.Option("root") ?? Environment.GetEnvironmentVariable("TESTSTACK_ROOT") ?? Directory.GetCurrentDirectory());
        var configPath = Path.Combine(root, args.Option("config") ?? Path.Combine("testing", "teststack.json"));
        var config = StackConfig.Load(configPath);

        var envPath = Path.Combine(root, config.Compose.EnvFile);
        var env = File.Exists(envPath) ? ReadEnvFile(envPath) : [];
        if (requireEnvFile && env.Count == 0)
        {
            throw new TestStackException($"Env file '{config.Compose.EnvFile}' not found; run 'testing/teststack build' first.");
        }

        return new StackContext(root, config, env);
    }

    public string ScenariosDirectory => Path.Combine(Root, Config.ScenariosDirectory);

    public string Variable(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } fromProcess ? fromProcess
        : _env.TryGetValue(name, out var value) ? value
        : throw new TestStackException($"Variable '{name}' is not set in {Config.Compose.EnvFile} or the environment.");

    public string PostgresConnectionString(string database) => new NpgsqlConnectionStringBuilder
    {
        Host = Config.Postgres.Host,
        Port = Config.Postgres.Port,
        Username = Config.Postgres.Username,
        Password = Variable(Config.Postgres.PasswordVariable),
        Database = database,
        // Pooled connections would block CREATE DATABASE ... TEMPLATE and DROP DATABASE.
        Pooling = false,
        Timeout = 5,
        CommandTimeout = 300,
        IncludeErrorDetail = true,
    }.ConnectionString;

    public async Task<NpgsqlConnection> OpenPostgresAsync(string database, CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(PostgresConnectionString(database));
        await connection.OpenAsync(ct);
        return connection;
    }

    public Compose Compose => new(this);

    internal static Dictionary<string, string> ReadEnvFile(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
            {
                value = value[1..^1];
            }

            values[line[..eq].Trim()] = value;
        }

        return values;
    }
}

internal sealed record ContainerState(string Service, string State, string? Health, int ExitCode);

/// <summary>Thin wrapper over <c>docker compose</c> for the test stack project.</summary>
internal sealed class Compose(StackContext context)
{
    public async Task<string> RunAsync(IEnumerable<string> arguments, CancellationToken ct = default)
    {
        var docker = Environment.GetEnvironmentVariable("TESTSTACK_DOCKER") ?? "docker";
        var psi = new ProcessStartInfo(docker)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = context.Root,
        };

        string[] prefix =
        [
            "compose",
            "-p", context.Config.Compose.Project,
            "-f", Path.Combine(context.Root, context.Config.Compose.File),
            "--env-file", Path.Combine(context.Root, context.Config.Compose.EnvFile),
        ];
        foreach (var arg in prefix.Concat(arguments))
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new TestStackException($"Failed to start '{docker}'.");
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new TestStackException($"'docker {string.Join(' ', psi.ArgumentList)}' failed ({process.ExitCode}): {(await stderr).Trim()}");
        }

        return await stdout;
    }

    public async Task<Dictionary<string, ContainerState>> PsAsync(CancellationToken ct = default)
    {
        var output = (await RunAsync(["ps", "--all", "--format", "json"], ct)).Trim();
        var states = new Dictionary<string, ContainerState>(StringComparer.Ordinal);
        if (output.Length == 0)
        {
            return states;
        }

        // Older compose versions print a JSON array, newer ones one JSON object per line.
        IEnumerable<JsonElement> items = output.StartsWith('[')
            ? JsonDocument.Parse(output).RootElement.EnumerateArray().ToList()
            : output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line).RootElement);

        foreach (var item in items)
        {
            var service = item.GetProperty("Service").GetString()!;
            states[service] = new ContainerState(
                service,
                item.TryGetProperty("State", out var state) ? state.GetString() ?? "" : "",
                item.TryGetProperty("Health", out var health) ? health.GetString() : null,
                item.TryGetProperty("ExitCode", out var exitCode) && exitCode.ValueKind == JsonValueKind.Number ? exitCode.GetInt32() : 0);
        }

        return states;
    }

    public Task StopAsync(IReadOnlyCollection<string> services, CancellationToken ct = default) =>
        services.Count == 0 ? Task.CompletedTask : RunAsync(["stop", .. services], ct);

    public Task StartAsync(IReadOnlyCollection<string> services, CancellationToken ct = default) =>
        services.Count == 0 ? Task.CompletedTask : RunAsync(["start", .. services], ct);

    public Task RestartAsync(IReadOnlyCollection<string> services, CancellationToken ct = default) =>
        services.Count == 0 ? Task.CompletedTask : RunAsync(["restart", .. services], ct);
}
