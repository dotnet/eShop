using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace eShop.TestStack;

internal abstract record PostgresStep(string Source);

/// <summary>Raw SQL file executed as-is (may contain several statements).</summary>
internal sealed record SqlStep(string Source, string Sql) : PostgresStep(Source);

/// <summary>Rows inserted into a table; keys are column names exactly as in the database.</summary>
internal sealed record TableStep(string Source, string Table, JsonArray Rows) : PostgresStep(Source);

internal sealed record RedisEntry(string Key, string Value, TimeSpan? Ttl);

internal sealed record ScenarioLayer(
    string Name,
    string FilePath,
    string? Description,
    IReadOnlyList<string> Extends,
    IReadOnlyList<(string Database, IReadOnlyList<PostgresStep> Steps)> Postgres,
    IReadOnlyList<RedisEntry> Redis);

/// <summary>
/// Scenario file format (YAML, or JSON since JSON is valid YAML):
/// <code>
/// description: text
/// extends: [other-scenario]          # optional, applied before this one
/// postgres:
///   &lt;database&gt;:                   # one of the configured databases, steps run in order
///     - sql: file.sql                  # path relative to the scenario file
///     - table: schema.Table            # or just Table for the public schema
///       rows: [ { Column: value } ]    # or rowsFile: rows.json|rows.yaml
/// redis:
///   - key: /basket/&lt;userId&gt;
///     json: { ... }                    # or value: "raw string"; optional ttl: seconds
/// </code>
/// </summary>
internal static partial class ScenarioParser
{
    private static readonly HashSet<string> TopLevelKeys = ["description", "extends", "postgres", "redis"];
    private static readonly HashSet<string> StepKeys = ["sql", "table", "rows", "rowsFile"];
    private static readonly HashSet<string> RedisKeys = ["key", "value", "json", "ttl"];

    /// <summary>
    /// Resolves a scenario name or path into the ordered layers to apply: the base layer first, then
    /// <c>extends</c> dependencies (depth first), then the scenario itself. Each file is applied once.
    /// </summary>
    public static IReadOnlyList<ScenarioLayer> Resolve(string scenario, string scenariosDirectory, string baseScenario, IReadOnlyCollection<string> databases)
    {
        var layers = new List<ScenarioLayer>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new Stack<string>();

        void Visit(string reference, string relativeTo)
        {
            var path = Locate(reference, relativeTo, scenariosDirectory);
            if (visited.Contains(path))
            {
                return;
            }

            if (visiting.Contains(path))
            {
                throw new TestStackException($"Scenario 'extends' cycle: {string.Join(" -> ", visiting.Reverse().Append(path))}", ExitCodes.InvalidScenario);
            }

            visiting.Push(path);
            var layer = ParseFile(path, databases);
            foreach (var dependency in layer.Extends)
            {
                Visit(dependency, Path.GetDirectoryName(path)!);
            }

            visiting.Pop();
            visited.Add(path);
            layers.Add(layer);
        }

        Visit(baseScenario, scenariosDirectory);
        Visit(scenario, Directory.GetCurrentDirectory());
        return layers;
    }

    /// <summary>A scenario reference is a file, a directory containing scenario.yaml, or a name under the scenarios directory.</summary>
    internal static string Locate(string reference, string relativeTo, string scenariosDirectory)
    {
        IEnumerable<string> Candidates(string root)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, reference));
            yield return candidate;
            yield return Path.Combine(candidate, "scenario.yaml");
            yield return Path.Combine(candidate, "scenario.yml");
            yield return Path.Combine(candidate, "scenario.json");
        }

        foreach (var candidate in Candidates(relativeTo).Concat(Candidates(scenariosDirectory)))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var available = Directory.Exists(scenariosDirectory)
            ? string.Join(", ", Directory.GetDirectories(scenariosDirectory).Select(Path.GetFileName).Order())
            : "(none)";
        throw new TestStackException($"Scenario '{reference}' not found. Available: {available}", ExitCodes.InvalidScenario);
    }

    public static ScenarioLayer ParseFile(string path, IReadOnlyCollection<string> databases)
    {
        var directory = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path) is "scenario" ? Path.GetFileName(directory) : Path.GetFileNameWithoutExtension(path);
        var root = LoadYaml(path);
        if (root is null)
        {
            return new ScenarioLayer(name, path, null, [], [], []);
        }

        var document = root as JsonObject ?? throw Invalid(path, "the document must be a mapping.");
        CheckKeys(document, TopLevelKeys, path, "top level");

        var extends = document["extends"] switch
        {
            null => [],
            JsonValue single => [single.ToString()],
            JsonArray many => many.Select(item => item?.ToString() ?? throw Invalid(path, "'extends' entries must be strings.")).ToList(),
            _ => throw Invalid(path, "'extends' must be a string or a list of strings."),
        };

        var postgres = new List<(string, IReadOnlyList<PostgresStep>)>();
        if (document["postgres"] is { } postgresNode)
        {
            var byDatabase = postgresNode as JsonObject ?? throw Invalid(path, "'postgres' must map database names to step lists.");
            foreach (var (database, stepsNode) in byDatabase)
            {
                if (!databases.Contains(database))
                {
                    throw Invalid(path, $"unknown database '{database}'. Known: {string.Join(", ", databases)}.");
                }

                var steps = (stepsNode as JsonArray ?? throw Invalid(path, $"postgres.{database} must be a list of steps."))
                    .Select((step, index) => ParseStep(step, directory, path, $"postgres.{database}[{index}]"))
                    .ToList();
                postgres.Add((database, steps));
            }
        }

        var redis = new List<RedisEntry>();
        if (document["redis"] is { } redisNode)
        {
            var entries = redisNode as JsonArray ?? throw Invalid(path, "'redis' must be a list.");
            for (var i = 0; i < entries.Count; i++)
            {
                redis.Add(ParseRedisEntry(entries[i], path, $"redis[{i}]"));
            }
        }

        return new ScenarioLayer(name, path, document["description"]?.ToString(), extends, postgres, redis);
    }

    private static PostgresStep ParseStep(JsonNode? node, string directory, string path, string location)
    {
        var step = node as JsonObject ?? throw Invalid(path, $"{location} must be a mapping with 'sql' or 'table'.");
        CheckKeys(step, StepKeys, path, location);

        if (step["sql"] is { } sqlNode)
        {
            if (step.Count != 1)
            {
                throw Invalid(path, $"{location}: 'sql' steps can't have other keys.");
            }

            var sqlPath = Path.GetFullPath(Path.Combine(directory, sqlNode.ToString()));
            if (!File.Exists(sqlPath))
            {
                throw Invalid(path, $"{location}: SQL file not found: {sqlPath}");
            }

            return new SqlStep($"{location} ({Path.GetFileName(sqlPath)})", File.ReadAllText(sqlPath));
        }

        var table = step["table"]?.ToString() ?? throw Invalid(path, $"{location} needs 'sql' or 'table'.");
        ParseQualifiedName(table, path, location);

        JsonArray rows;
        if (step["rowsFile"] is { } rowsFile)
        {
            if (step.ContainsKey("rows"))
            {
                throw Invalid(path, $"{location}: use either 'rows' or 'rowsFile'.");
            }

            var rowsPath = Path.GetFullPath(Path.Combine(directory, rowsFile.ToString()));
            if (!File.Exists(rowsPath))
            {
                throw Invalid(path, $"{location}: rows file not found: {rowsPath}");
            }

            rows = LoadYaml(rowsPath) as JsonArray ?? throw Invalid(rowsPath, "a rows file must contain a list of rows.");
        }
        else
        {
            rows = step["rows"] as JsonArray ?? throw Invalid(path, $"{location}: 'rows' must be a list.");
        }

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i] is not JsonObject row || row.Count == 0)
            {
                throw Invalid(path, $"{location}.rows[{i}] must be a non-empty mapping of column: value.");
            }
        }

        return new TableStep($"{location} ({table})", table, (JsonArray)rows.DeepClone());
    }

    private static RedisEntry ParseRedisEntry(JsonNode? node, string path, string location)
    {
        var entry = node as JsonObject ?? throw Invalid(path, $"{location} must be a mapping.");
        CheckKeys(entry, RedisKeys, path, location);

        var key = entry["key"]?.ToString() ?? throw Invalid(path, $"{location}: 'key' is required.");
        var hasJson = entry.ContainsKey("json");
        var hasValue = entry.ContainsKey("value");
        if (hasJson == hasValue)
        {
            throw Invalid(path, $"{location}: set exactly one of 'value' or 'json'.");
        }

        var value = hasJson
            ? entry["json"]?.ToJsonString() ?? "null"
            : entry["value"]?.ToString() ?? "";

        TimeSpan? ttl = null;
        if (entry["ttl"] is { } ttlNode)
        {
            ttl = int.TryParse(ttlNode.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : throw Invalid(path, $"{location}: 'ttl' must be a positive number of seconds.");
        }

        return new RedisEntry(key, value, ttl);
    }

    /// <summary>Splits <c>schema.table</c> (or <c>table</c>) into a quoted, case-preserving identifier.</summary>
    internal static string ParseQualifiedName(string name, string path = "", string location = "")
    {
        var parts = name.Split('.');
        if (parts.Length is < 1 or > 2 || parts.Any(string.IsNullOrWhiteSpace))
        {
            throw Invalid(path, $"{location}: invalid table name '{name}'. Use 'Table' or 'schema.Table'.");
        }

        return string.Join('.', parts.Select(QuoteIdentifier));
    }

    internal static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    private static void CheckKeys(JsonObject node, HashSet<string> allowed, string path, string location)
    {
        foreach (var key in node.Select(pair => pair.Key))
        {
            if (!allowed.Contains(key))
            {
                throw Invalid(path, $"{location}: unknown key '{key}'. Allowed: {string.Join(", ", allowed)}.");
            }
        }
    }

    private static TestStackException Invalid(string path, string message) =>
        new($"Invalid scenario {path}: {message}", ExitCodes.InvalidScenario);

    internal static JsonNode? LoadYaml(string path)
    {
        var stream = new YamlStream();
        try
        {
            using var reader = new StreamReader(path);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            throw Invalid(path, $"YAML error at line {ex.Start.Line}, column {ex.Start.Column}: {ex.InnerException?.Message ?? ex.Message}");
        }

        return stream.Documents.Count == 0 ? null : ToJson(stream.Documents[0].RootNode);
    }

    /// <summary>
    /// Converts YAML to JSON. Unquoted scalars that look like JSON numbers/booleans/null keep that type; everything
    /// else (including quoted scalars and values like "02134") stays a string. Postgres converts strings to the
    /// column type, so timestamps, uuids and vectors can be written as plain strings.
    /// </summary>
    internal static JsonNode? ToJson(YamlNode node) => node switch
    {
        YamlMappingNode mapping => new JsonObject(mapping.Children.Select(pair =>
            KeyValuePair.Create(((YamlScalarNode)pair.Key).Value ?? "", ToJson(pair.Value)))),
        YamlSequenceNode sequence => new JsonArray(sequence.Children.Select(ToJson).ToArray()),
        YamlScalarNode scalar => ScalarToJson(scalar),
        _ => throw new TestStackException($"Unsupported YAML node at {node.Start}.", ExitCodes.InvalidScenario),
    };

    private static JsonNode? ScalarToJson(YamlScalarNode scalar)
    {
        var value = scalar.Value ?? "";
        if (scalar.Style != ScalarStyle.Plain)
        {
            return JsonValue.Create(value);
        }

        return value switch
        {
            "" or "~" or "null" or "Null" or "NULL" => null,
            "true" or "True" or "TRUE" => JsonValue.Create(true),
            "false" or "False" or "FALSE" => JsonValue.Create(false),
            _ when JsonNumber().IsMatch(value) => JsonNode.Parse(value),
            _ => JsonValue.Create(value),
        };
    }

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$")]
    private static partial Regex JsonNumber();
}
