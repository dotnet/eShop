# eShop.ServiceDefaults

## Overview
The eShop.ServiceDefaults project provides shared configuration and extensions for all microservices in the eShop application. It standardizes observability, resilience, and health check configurations.

## Technology Stack
- **Observability**: OpenTelemetry
- **Resilience**: Polly (via Microsoft.Extensions.Http.Resilience)
- **Health Checks**: ASP.NET Core Health Checks

## Purpose
- Centralize common service configuration
- Ensure consistent observability across services
- Provide standard health check patterns
- Configure resilience policies

## Key Extensions

### AddServiceDefaults
Main extension method that configures:
- OpenTelemetry (tracing, metrics, logging)
- Health checks
- Service discovery
- Default HTTP client resilience

```csharp
builder.AddServiceDefaults();
```

### MapDefaultEndpoints
Maps standard health and diagnostic endpoints:
- `/health` - Liveness probe
- `/alive` - Readiness probe

```csharp
app.MapDefaultEndpoints();
```

## OpenTelemetry Configuration

### Tracing
- ASP.NET Core instrumentation
- HTTP client instrumentation
- Entity Framework Core instrumentation
- RabbitMQ instrumentation
- Custom activity sources

### Metrics
- ASP.NET Core metrics
- HTTP client metrics
- Runtime metrics
- Custom meters

### Logging
- OTLP log export
- Structured logging

## Health Checks
| Check | Purpose |
|-------|---------|
| Default | Service is running |
| Database | Database connectivity |
| Redis | Cache connectivity |
| RabbitMQ | Message broker connectivity |

## Project Structure
```
eShop.ServiceDefaults/
├── Extensions.cs            # Main extension methods
└── eShop.ServiceDefaults.csproj
```

## Usage in Services
Every service references this project:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add standard service configuration
builder.AddServiceDefaults();

// ... service-specific configuration

var app = builder.Build();

// Map health endpoints
app.MapDefaultEndpoints();

// ... service-specific middleware
```

## HTTP Client Resilience
Configures default resilience policies:
- Retry with exponential backoff
- Circuit breaker
- Timeout policies

## Service Discovery
Enables automatic service discovery:
- Services resolve by name
- No hardcoded URLs
- Works with Aspire orchestration

## OTLP Export
Telemetry is exported to:
- Aspire Dashboard (local development)
- Azure Monitor (production)
- Any OTLP-compatible backend

## Dependencies
- **OpenTelemetry.* packages**: Instrumentation and export
- **Microsoft.Extensions.Http.Resilience**: HTTP resilience
- **Microsoft.Extensions.ServiceDiscovery**: Service discovery
- **AspNetCore.HealthChecks.***: Health check implementations

## Configuration
Telemetry endpoint configured via environment:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

## Notes
- Referenced by all service projects
- Ensures consistency across the application
- Simplifies service boilerplate
- Centralizes observability configuration
