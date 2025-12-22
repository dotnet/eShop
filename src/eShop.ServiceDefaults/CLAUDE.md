# eShop.ServiceDefaults

## Purpose
Shared Aspire service configuration library that provides common settings and extensions used by all microservices.

## Key Responsibilities
- Configure OpenTelemetry for distributed tracing
- Set up health checks endpoints
- Configure service discovery
- Provide common HTTP client configurations
- Set up resilience and retry policies

## Usage
All API projects reference this and call:
```csharp
builder.AddServiceDefaults();
```

## Key Extensions
- Health checks at `/health` and `/alive`
- OpenTelemetry tracing and metrics
- Service discovery integration
