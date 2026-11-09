using System.Text.Json;
using System.Text.Json.Serialization;

namespace eShop.TestStack;

/// <summary>Strongly-typed view of <c>testing/teststack.json</c>.</summary>
internal sealed class StackConfig
{
    public required ComposeConfig Compose { get; init; }
    public string ScenariosDirectory { get; init; } = "testing/scenarios";
    public string BaseScenario { get; init; } = "_base";
    public required PostgresConfig Postgres { get; init; }
    public required RedisConfig Redis { get; init; }
    public required RabbitMqConfig RabbitMq { get; init; }
    public List<HiLoSequenceConfig> HiLoSequences { get; init; } = [];
    public List<string> InfrastructureServices { get; init; } = [];
    public List<ServiceConfig> Services { get; init; } = [];

    /// <summary>Seconds a running-but-unready service may stay unready before <c>wait</c> restarts it once (0 disables).</summary>
    public int HealAfterSeconds { get; init; } = 60;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static StackConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new TestStackException($"Config file not found: {path}", ExitCodes.Usage);
        }

        return JsonSerializer.Deserialize<StackConfig>(File.ReadAllText(path), JsonOptions)
            ?? throw new TestStackException($"Config file is empty: {path}", ExitCodes.Usage);
    }
}

internal sealed class ComposeConfig
{
    public required string Project { get; init; }
    public required string File { get; init; }
    public required string EnvFile { get; init; }
}

internal sealed class PostgresConfig
{
    public string Host { get; init; } = "localhost";
    public required int Port { get; init; }
    public string Username { get; init; } = "postgres";
    public required string PasswordVariable { get; init; }
    public required List<string> Databases { get; init; }
}

internal sealed class RedisConfig
{
    public string Host { get; init; } = "localhost";
    public required int Port { get; init; }
    public required string PasswordVariable { get; init; }
}

internal sealed class RabbitMqConfig
{
    public string Host { get; init; } = "localhost";
    public required int Port { get; init; }
    public string Username { get; init; } = "guest";
    public required string PasswordVariable { get; init; }
    public List<string> Queues { get; init; } = [];
}

/// <summary>
/// An EF Core HiLo sequence (not owned by a column). After a load it's moved past MAX(column); the owning
/// service caches ID blocks in memory, so it's restarted when the sequence was used since the last load.
/// </summary>
internal sealed class HiLoSequenceConfig
{
    public required string Database { get; init; }
    public required string Sequence { get; init; }
    public required string Table { get; init; }
    public required string Column { get; init; }
    public required string Service { get; init; }
}

/// <summary>Readiness definition for one compose service.</summary>
internal sealed class ServiceConfig
{
    public required string Name { get; init; }

    /// <summary>Host URL that must answer with a status below 500.</summary>
    public string? Url { get; init; }

    /// <summary>Probe with HTTP/2 prior knowledge (gRPC-only Kestrel endpoints).</summary>
    public bool Http2 { get; init; }

    /// <summary>Databases whose EF migrations this service applies at startup.</summary>
    public List<string> Databases { get; init; } = [];

    /// <summary>Event bus queues this service consumes (must exist with at least one consumer).</summary>
    public List<string> Queues { get; init; } = [];
}
