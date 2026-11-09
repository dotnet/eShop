using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;

namespace eShop.TestStack;

internal sealed record CheckResult(string Name, bool Ok, string Detail)
{
    public JsonObject ToJson() => new() { ["name"] = Name, ["ok"] = Ok, ["detail"] = Detail };
}

/// <summary>
/// Readiness for the whole stack. The services run with production settings, so /health isn't mapped
/// (ServiceDefaults only maps it in Development). Instead a service is ready when:
/// its container is running; its port answers HTTP (Kestrel only listens after the EF migration hosted
/// service has finished StartAsync); its databases have EF migration history; and its event bus queue exists
/// with a consumer (so integration events published by tests aren't dropped).
/// </summary>
internal sealed class Readiness(StackContext context)
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(2),
        UseCookies = false,
    })
    {
        Timeout = TimeSpan.FromSeconds(5),
    };

    public async Task<IReadOnlyList<CheckResult>> CheckAsync(CancellationToken ct)
    {
        var results = new List<CheckResult>();
        var config = context.Config;

        Dictionary<string, ContainerState> containers;
        try
        {
            containers = await context.Compose.PsAsync(ct);
        }
        catch (TestStackException ex)
        {
            return [new CheckResult("compose", false, ex.Message)];
        }

        foreach (var service in config.InfrastructureServices.Concat(config.Services.Select(s => s.Name)))
        {
            results.Add(containers.TryGetValue(service, out var state)
                ? new CheckResult($"container:{service}", state.State == "running", state.State == "running" ? "running" : $"{state.State} (exit {state.ExitCode})")
                : new CheckResult($"container:{service}", false, "not created; run 'testing/teststack up'"));
        }

        results.Add(await Probe("postgres", async () =>
        {
            await using var connection = await context.OpenPostgresAsync("postgres", ct);
            return "accepting connections";
        }));
        results.Add(await Probe("redis", async () =>
        {
            await new RedisStore(context).PingAsync();
            return "PONG";
        }));

        IReadOnlyList<QueueState>? queues = null;
        results.Add(await Probe("eventbus", async () =>
        {
            queues = await new RabbitMqStore(context).InspectAsync(ct);
            return "accepting connections";
        }));

        var store = new PostgresStore(context);
        foreach (var service in config.Services)
        {
            if (service.Url is { } url)
            {
                results.Add(await Probe($"http:{service.Name}", () => ProbeHttpAsync(url, service.Http2, ct)));
            }

            foreach (var database in service.Databases)
            {
                results.Add(await Probe($"migrations:{database}", async () =>
                {
                    var migrations = await store.MigrationsAsync(database, ct);
                    return migrations is { Count: > 0 }
                        ? $"{migrations.Count} applied (latest {migrations[^1]})"
                        : throw new InvalidOperationException("no migrations applied yet");
                }));
            }

            foreach (var queue in service.Queues)
            {
                var state = queues?.FirstOrDefault(q => q.Queue == queue);
                results.Add(state switch
                {
                    null => new CheckResult($"queue:{queue}", false, "event bus unavailable"),
                    { Exists: false } => new CheckResult($"queue:{queue}", false, $"not declared yet by {service.Name}"),
                    { Consumers: 0 } => new CheckResult($"queue:{queue}", false, $"no consumer ({service.Name} not subscribed)"),
                    _ => new CheckResult($"queue:{queue}", true, $"{state.Consumers} consumer(s), {state.Messages} message(s)"),
                });
            }
        }

        return results;
    }

    /// <summary>Polls until every check passes. Restarts a service once if it stays running-but-unready too long.</summary>
    public async Task<(bool Ready, IReadOnlyList<CheckResult> Checks, IReadOnlyList<string> Healed)> WaitAsync(TimeSpan timeout, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var unreadySince = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);
        var healed = new List<string>();
        var lastSummary = "";

        while (true)
        {
            var checks = await CheckAsync(ct);
            var failing = checks.Where(c => !c.Ok).ToList();
            if (failing.Count == 0)
            {
                return (true, checks, healed);
            }

            var summary = string.Join(", ", failing.Select(c => c.Name));
            if (summary != lastSummary)
            {
                Output.Log($"waiting ({stopwatch.Elapsed.TotalSeconds:F0}s): {summary}");
                lastSummary = summary;
            }

            if (stopwatch.Elapsed >= timeout)
            {
                return (false, checks, healed);
            }

            await HealAsync(checks, unreadySince, healed, stopwatch.Elapsed, ct);
            await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        }
    }

    // A service whose container runs but whose probes keep failing (e.g. the event bus consumer failed to
    // start because RabbitMQ wasn't up yet) is restarted once.
    private async Task HealAsync(IReadOnlyList<CheckResult> checks, Dictionary<string, TimeSpan> unreadySince, List<string> healed, TimeSpan now, CancellationToken ct)
    {
        if (context.Config.HealAfterSeconds <= 0)
        {
            return;
        }

        foreach (var service in context.Config.Services)
        {
            var running = checks.Any(c => c.Name == $"container:{service.Name}" && c.Ok);
            var serviceChecks = checks.Where(c =>
                c.Name == $"http:{service.Name}" ||
                service.Queues.Any(q => c.Name == $"queue:{q}") ||
                service.Databases.Any(d => c.Name == $"migrations:{d}")).ToList();
            var infraReady = checks.Where(c => c.Name is "postgres" or "redis" or "eventbus").All(c => c.Ok);

            if (!running || !infraReady || serviceChecks.All(c => c.Ok))
            {
                unreadySince.Remove(service.Name);
                continue;
            }

            unreadySince.TryAdd(service.Name, now);
            if (now - unreadySince[service.Name] >= TimeSpan.FromSeconds(context.Config.HealAfterSeconds) && !healed.Contains(service.Name))
            {
                Output.Log($"restarting {service.Name}: unready for {context.Config.HealAfterSeconds}s ({string.Join("; ", serviceChecks.Where(c => !c.Ok).Select(c => $"{c.Name}: {c.Detail}"))})");
                await context.Compose.RestartAsync([service.Name], ct);
                healed.Add(service.Name);
                unreadySince.Remove(service.Name);
            }
        }
    }

    private static async Task<string> ProbeHttpAsync(string url, bool http2, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (http2)
        {
            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        }

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        return (int)response.StatusCode < 500
            ? $"HTTP {(int)response.StatusCode}"
            : throw new InvalidOperationException($"HTTP {(int)response.StatusCode}");
    }

    private static async Task<CheckResult> Probe(string name, Func<Task<string>> probe)
    {
        try
        {
            return new CheckResult(name, true, await probe());
        }
        catch (Exception ex) when (ex is not OperationCanceledException || ex is TaskCanceledException)
        {
            return new CheckResult(name, false, ex.GetBaseException().Message);
        }
    }
}
