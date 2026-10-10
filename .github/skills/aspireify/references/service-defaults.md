# ServiceDefaults Wiring

After resources are declared in the AppHost, every service project should opt
into Aspire's defaults: **OpenTelemetry**, **health checks**, **service
discovery**, and **HTTP resilience**. This is what `Aspire.ServiceDefaults` does.

## Add the Project

`aspire init` may already have generated a `MyApp.ServiceDefaults` project. If
not, create one:

```bash
dotnet new aspire-servicedefaults -n MyApp.ServiceDefaults
```

Then reference it from each service:

```bash
dotnet add ./src/Api/Api.csproj reference ./MyApp.ServiceDefaults/MyApp.ServiceDefaults.csproj
```

## Wire It in `Program.cs`

Each service's `Program.cs` should call `AddServiceDefaults()` and
`MapDefaultEndpoints()`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();   // OTel, health checks, service discovery, HTTP resilience

// ... your services ...

var app = builder.Build();

app.MapDefaultEndpoints();      // /health and /alive endpoints

// ... your endpoints ...

app.Run();
```

## What `AddServiceDefaults()` Wires

| Concern | What |
|---------|------|
| OpenTelemetry tracing | ASP.NET Core, HttpClient, gRPC — exported via OTLP to dashboard |
| OpenTelemetry metrics | Runtime, ASP.NET Core, HttpClient — exported via OTLP |
| OpenTelemetry logging | Structured logs exported via OTLP |
| Health checks | `IHealthChecksBuilder` configured; `/health` and `/alive` endpoints |
| Service discovery | `Microsoft.Extensions.ServiceDiscovery` registered for `HttpClient` |
| HTTP resilience | Standard resilience handler on outbound `HttpClient` |

## Wire Health Checks for `WaitFor`

`AppHost`'s `.WaitFor(resource)` requires a health endpoint. `MapDefaultEndpoints()`
provides `/alive` (liveness) and `/health` (readiness).

In the AppHost:

```csharp
var api = builder.AddProject<Projects.Api>("api")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Worker>("worker")
    .WithReference(api)
    .WaitFor(api);   // Blocks worker start until api /health is 200 OK
```

## Frontend Projects (Node / Next.js / Vite)

Service discovery for JS frontends comes via env vars injected by
`WithReference(api)`:

```
services__api__http__0=http://localhost:NNNN
services__api__https__0=https://localhost:NNNN
```

In the frontend, read these (or use `endpoint.url` in TS AppHost expressions to
pre-resolve at AppHost build time).

For browser-side telemetry, add `WithBrowserLogs()` on the frontend resource —
it captures console logs, network requests, and screenshots into the dashboard
without any client-side wiring.

## Custom OTel Configuration

`AddServiceDefaults()` is a starting point. Extend it in your own
`ServiceDefaults` project:

```csharp
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    builder.ConfigureOpenTelemetry();
    builder.AddDefaultHealthChecks();
    builder.Services.AddServiceDiscovery();
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
    });
    return builder;
}
```

## Checklist

- [ ] `Aspire.ServiceDefaults` project exists
- [ ] Each service `.csproj` has `<ProjectReference>` to it
- [ ] Each service `Program.cs` calls `builder.AddServiceDefaults()`
- [ ] Each service `Program.cs` calls `app.MapDefaultEndpoints()`
- [ ] AppHost uses `.WithHttpHealthCheck("/health")` for resources that consumers `WaitFor`
- [ ] Frontend resources use `.WithBrowserLogs()` if browser telemetry is desired
