using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;

namespace eShop.TestStack;

internal static class Commands
{
    private const int DefaultTimeoutSeconds = 300;

    public static async Task<int> RunAsync(ParsedArgs args, CancellationToken ct)
    {
        switch (args.Command)
        {
            case "wait": return await WaitAsync(args, ct);
            case "status": return await StatusAsync(args, ct);
            case "reset": return await LoadAsync(args, scenario: null, ct);
            case "load":
                if (args.Positionals.Count != 1)
                {
                    throw new TestStackException("Usage: load <scenario> [--hard] [--timeout <seconds>]", ExitCodes.Usage);
                }

                return await LoadAsync(args, args.Positionals[0], ct);
            case "validate": return Validate(args);
            case "hash-password": return HashPassword(args);
            default:
                Console.Error.WriteLine("""
                    Usage: teststack <command> [options]
                      wait [--timeout s]                  Wait until every service, database and queue is ready.
                      status                              One readiness pass (no waiting).
                      reset [--hard]                      Clear all state, then load the base scenario.
                      load <scenario> [--hard]            Clear all state, then load base + scenario.
                      validate [scenario...]              Parse scenarios (default: all) without touching the stack.
                      hash-password <password>            ASP.NET Identity V3 hash for AspNetUsers.PasswordHash.
                    Common options: --root <repo>, --config <file>, --timeout <seconds> (default 300), --no-wait
                    stdout: one JSON result. Exit codes: 0 ok, 1 error, 2 not ready, 3 invalid scenario, 64 usage.
                    """);
                return ExitCodes.Usage;
        }
    }

    private static async Task<int> WaitAsync(ParsedArgs args, CancellationToken ct)
    {
        var context = StackContext.Create(args);
        var stopwatch = Stopwatch.StartNew();
        var (ready, checks, healed) = await new Readiness(context).WaitAsync(Timeout(args), ct);
        Output.Result(new JsonObject
        {
            ["ok"] = ready,
            ["command"] = "wait",
            ["elapsedMs"] = stopwatch.ElapsedMilliseconds,
            ["restarted"] = ToJsonArray(healed),
            ["failing"] = new JsonArray(checks.Where(c => !c.Ok).Select(c => (JsonNode)c.ToJson()).ToArray()),
        });
        return ready ? ExitCodes.Ok : ExitCodes.NotReady;
    }

    private static async Task<int> StatusAsync(ParsedArgs args, CancellationToken ct)
    {
        var context = StackContext.Create(args);
        var checks = await new Readiness(context).CheckAsync(ct);
        var ready = checks.All(c => c.Ok);
        Output.Result(new JsonObject
        {
            ["ok"] = ready,
            ["command"] = "status",
            ["checks"] = new JsonArray(checks.Select(c => (JsonNode)c.ToJson()).ToArray()),
        });
        return ready ? ExitCodes.Ok : ExitCodes.NotReady;
    }

    /// <summary>
    /// Soft (default): purge queues, truncate tables, flush Redis, purge again, load layers, sync sequences.
    /// Hard: stop the app services, recreate each database from its template (a snapshot of the freshly
    /// migrated, empty schema), flush/purge, start the services and then load the layers.
    /// EF migration history is never modified in either mode.
    /// </summary>
    private static async Task<int> LoadAsync(ParsedArgs args, string? scenario, CancellationToken ct)
    {
        var context = StackContext.Create(args);
        var config = context.Config;
        var hard = args.Flag("hard");
        var timeout = Timeout(args);
        var stopwatch = Stopwatch.StartNew();

        var layers = ScenarioParser.Resolve(scenario ?? config.BaseScenario, context.ScenariosDirectory, config.BaseScenario, config.Postgres.Databases);
        Output.Log($"layers: {string.Join(" -> ", layers.Select(l => l.Name))} ({(hard ? "hard" : "soft")} reset)");

        var readiness = new Readiness(context);
        // Hard mode restarts every app service and waits afterwards, so it can also recover a broken stack.
        if (!hard && !args.Flag("no-wait"))
        {
            await EnsureReadyAsync(readiness, timeout, "before reset", ct);
        }

        var postgres = new PostgresStore(context);
        var redis = new RedisStore(context);
        var rabbit = new RabbitMqStore(context);
        var restarted = new List<string>();
        uint purged = 0;

        if (hard)
        {
            var appServices = config.Services.Select(s => s.Name).ToList();
            Output.Log($"stopping {appServices.Count} app services");
            await context.Compose.StopAsync(appServices, ct);

            var missingTemplates = new List<string>();
            foreach (var database in config.Postgres.Databases)
            {
                var template = database + PostgresStore.TemplateSuffix;
                var current = await postgres.MigrationsAsync(database, ct);
                var snapshot = await postgres.MigrationsAsync(template, ct);
                if (snapshot is not null && current is not null && snapshot.SequenceEqual(current))
                {
                    await postgres.CopyDatabaseAsync(template, database, ct);
                    Output.Log($"{database}: restored from {template}");
                }
                else
                {
                    // No (or outdated) template: drop the database so the service recreates it from its migrations.
                    await postgres.DropDatabaseAsync(database, ct);
                    missingTemplates.Add(database);
                    Output.Log($"{database}: dropped; migrations will recreate it (template {(snapshot is null ? "missing" : "outdated")})");
                }
            }

            await redis.FlushAsync();
            purged += await rabbit.PurgeAsync(ct);

            await context.Compose.StartAsync(appServices, ct);
            restarted.AddRange(appServices);
            await EnsureReadyAsync(readiness, timeout, "after restoring databases", ct);

            if (missingTemplates.Count > 0)
            {
                Output.Log($"snapshotting templates for {string.Join(", ", missingTemplates)}");
                await context.Compose.StopAsync(appServices, ct);
                foreach (var database in missingTemplates)
                {
                    await postgres.TruncateAsync(database, ct);
                    await postgres.CopyDatabaseAsync(database, database + PostgresStore.TemplateSuffix, ct);
                }

                await context.Compose.StartAsync(appServices, ct);
                await EnsureReadyAsync(readiness, timeout, "after snapshotting templates", ct);
            }

            purged += await rabbit.PurgeAsync(ct);
        }
        else
        {
            // Services with EF HiLo sequences cache ID blocks in memory; if they used a block since the last load,
            // restart them so they don't hand out IDs that collide with the new scenario's rows.
            var stale = await postgres.ServicesWithDirtyHiLoAsync(ct);
            if (stale.Count > 0)
            {
                Output.Log($"stopping {string.Join(", ", stale)} (HiLo ID cache)");
                await context.Compose.StopAsync(stale, ct);
            }

            purged += await rabbit.PurgeAsync(ct);
            foreach (var database in config.Postgres.Databases)
            {
                await postgres.TruncateAsync(database, ct);
            }

            await redis.FlushAsync();
            purged += await rabbit.PurgeAsync(ct);

            await ApplyLayersAsync(context, layers, ct);

            if (stale.Count > 0)
            {
                await context.Compose.StartAsync(stale, ct);
                restarted.AddRange(stale);
                await EnsureReadyAsync(readiness, timeout, "after restarting HiLo services", ct);
            }

            return Report(scenario, layers, hard, restarted, purged, stopwatch);
        }

        await ApplyLayersAsync(context, layers, ct);
        return Report(scenario, layers, hard, restarted, purged, stopwatch);
    }

    private static int Report(string? scenario, IReadOnlyList<ScenarioLayer> layers, bool hard, List<string> restarted, uint purged, Stopwatch stopwatch)
    {
        Output.Result(new JsonObject
        {
            ["ok"] = true,
            ["command"] = scenario is null ? "reset" : "load",
            ["scenario"] = scenario ?? layers[^1].Name,
            ["layers"] = ToJsonArray(layers.Select(l => l.Name)),
            ["mode"] = hard ? "hard" : "soft",
            ["purgedMessages"] = purged,
            ["restarted"] = ToJsonArray(restarted),
            ["elapsedMs"] = stopwatch.ElapsedMilliseconds,
        });
        return ExitCodes.Ok;
    }

    private static async Task ApplyLayersAsync(StackContext context, IReadOnlyList<ScenarioLayer> layers, CancellationToken ct)
    {
        var postgres = new PostgresStore(context);

        foreach (var database in context.Config.Postgres.Databases)
        {
            var steps = layers.SelectMany(l => l.Postgres.Where(p => p.Database == database).SelectMany(p => p.Steps)).ToList();
            if (steps.Count > 0)
            {
                var rows = await postgres.LoadAsync(database, steps, ct);
                Output.Log($"{database}: {steps.Count} step(s), {rows} row(s)");
            }

            await postgres.SyncSequencesAsync(database, ct);
        }

        var keys = await new RedisStore(context).LoadAsync(layers.SelectMany(l => l.Redis).ToList());
        if (keys > 0)
        {
            Output.Log($"redis: {keys} key(s)");
        }
    }

    private static async Task EnsureReadyAsync(Readiness readiness, TimeSpan timeout, string phase, CancellationToken ct)
    {
        var (ready, checks, _) = await readiness.WaitAsync(timeout, ct);
        if (!ready)
        {
            var failing = string.Join("; ", checks.Where(c => !c.Ok).Select(c => $"{c.Name}: {c.Detail}"));
            throw new TestStackException($"Stack not ready {phase} after {timeout.TotalSeconds:F0}s: {failing}", ExitCodes.NotReady);
        }
    }

    private static int Validate(ParsedArgs args)
    {
        var context = StackContext.Create(args, requireEnvFile: false);
        var config = context.Config;
        var scenarios = args.Positionals.Count > 0
            ? args.Positionals
            : Directory.GetDirectories(context.ScenariosDirectory).Select(Path.GetFileName).OfType<string>().Order().ToList();

        var results = new JsonArray();
        foreach (var scenario in scenarios)
        {
            var layers = ScenarioParser.Resolve(scenario, context.ScenariosDirectory, config.BaseScenario, config.Postgres.Databases);
            results.Add(new JsonObject
            {
                ["scenario"] = scenario,
                ["layers"] = ToJsonArray(layers.Select(l => l.Name)),
                ["postgresSteps"] = layers.Sum(l => l.Postgres.Sum(p => p.Steps.Count)),
                ["redisKeys"] = layers.Sum(l => l.Redis.Count),
            });
        }

        Output.Result(new JsonObject { ["ok"] = true, ["command"] = "validate", ["scenarios"] = results });
        return ExitCodes.Ok;
    }

    private static int HashPassword(ParsedArgs args)
    {
        if (args.Positionals.Count != 1)
        {
            throw new TestStackException("Usage: hash-password <password>", ExitCodes.Usage);
        }

        Output.Result(new JsonObject { ["ok"] = true, ["command"] = "hash-password", ["hash"] = Passwords.Hash(args.Positionals[0]) });
        return ExitCodes.Ok;
    }

    private static TimeSpan Timeout(ParsedArgs args) => TimeSpan.FromSeconds(args.IntOption("timeout", DefaultTimeoutSeconds));

    private static JsonArray ToJsonArray(IEnumerable<string> values) => new(values.Select(v => (JsonNode)JsonValue.Create(v)).ToArray());
}

/// <summary>Same hasher Identity.API uses (V3: PBKDF2-HMAC-SHA512, 100k iterations), so hashes verify at login.</summary>
internal static class Passwords
{
    private static readonly PasswordHasher<object> Hasher = new();

    public static string Hash(string password) => Hasher.HashPassword(new object(), password);

    public static bool Verify(string hash, string password) =>
        Hasher.VerifyHashedPassword(new object(), hash, password) != PasswordVerificationResult.Failed;
}
