# eShop.ServiceDefaults

Shared library providing standardized cross-cutting concern configurations for all microservices in the eShop platform: authentication, observability, resilience, health checks, and OpenAPI documentation.

## Summary

- Centralized configuration library used by all eShop microservices to reduce code duplication and ensure consistency
- Provides extension methods for ASP.NET Core dependency injection and middleware configuration
- Enables JWT Bearer authentication with Identity Server integration, OpenTelemetry observability (tracing, metrics, logs), resilience handlers, service discovery, and health check endpoints
- Built on .NET 10.0 with no runtime dependencies beyond ASP.NET Core framework

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|-------------|
| `src/eShop.ServiceDefaults/` | Shared library for cross-cutting defaults and extension methods | Extensions.cs, AuthenticationExtensions.cs, HttpClientExtensions.cs, OpenApi.Extensions.cs |

## Components

---

COMPONENT_NAME: ServiceDefaults
TYPE: Utility
PURPOSE: Main orchestrator providing extension methods for configuring standard services, health checks, resilience, and OpenTelemetry observability across all microservices.
RESPONSIBILITIES:
  - Configure service discovery, HTTP resilience policies, and health checks via `AddServiceDefaults()`
  - Provide lightweight variant `AddBasicServiceDefaults()` for background services
  - Configure OpenTelemetry logging, tracing, and metrics with OTLP exporter support
  - Map diagnostic endpoints including liveness (`/alive`) and readiness (`/health`) checks
CALLS:
  - `System.Dependencies` — framework references for ASP.NET Core and OpenTelemetry
CALLED_BY:
  - Catalog.API
  - Webhooks.API
  - Identity.API
  - WebhookClient
  - Ordering.API
  - WebApp
  - Basket.API
  - PaymentProcessor
  - OrderProcessor

---

COMPONENT_NAME: AuthenticationExtensions
TYPE: Utility
PURPOSE: Configure JWT Bearer authentication with federated identity via Identity Server.
RESPONSIBILITIES:
  - Parse Identity configuration section for server URL and audience
  - Register JWT Bearer authentication handler with Authority, Audience, and token validation parameters
  - Prevent "sub" claim mapping to nameidentifier per OpenID Connect conventions
  - Support Android Emulator issuer validation in DEBUG mode
  - Add authorization policy service to the container
SOURCE: `src/eShop.ServiceDefaults/AuthenticationExtensions.cs`
CALLS:
  - ExtensionMethods — calls chainable service extension methods
CALLED_BY:
  - Services that require `AddDefaultAuthentication()` (e.g., Webhooks.API, Ordering.API with authorization)

---

COMPONENT_NAME: HttpClientExtensions
TYPE: Utility
PURPOSE: Configure HTTP clients with automatic authorization token propagation for inter-service calls.
RESPONSIBILITIES:
  - Register `HttpClientAuthorizationDelegatingHandler` as transient message handler
  - Inject delegating handler into HTTP client pipeline via `AddAuthToken()`
  - Extract Bearer token from current HttpContext and append to outbound requests
  - Access token obtained from `context.GetTokenAsync("access_token")`
SOURCE: `src/eShop.ServiceDefaults/HttpClientExtensions.cs`
CALLS:
  - `HttpContext` — to retrieve access token
  - `IHttpClientBuilder` — to register message handler
CALLED_BY:
  - Services configuring typed HTTP clients (Basket.API, Ordering.API)

---

COMPONENT_NAME: OpenApiExtensions
TYPE: Utility
PURPOSE: Configure OpenAPI (Swagger) documentation generation and UI with API versioning support.
RESPONSIBILITIES:
  - Register OpenAPI document generators and Scalar UI middleware via `AddDefaultOpenApi()`
  - Map OpenAPI endpoints and Scalar UI routes via `UseDefaultOpenApi()`
  - Integrate with Asp.Versioning to format API versions as "'v'major[.minor][-status]"
  - Parse OpenApi configuration section and read OAuth scopes from Identity section
  - Only activate if OpenApi section exists in configuration
SOURCE: `src/eShop.ServiceDefaults/OpenApi.Extensions.cs`
CALLS:
  - OpenApiOptions — to apply document and operation transformations
  - IApiVersioningBuilder — to integrate with API versioning framework
CALLED_BY:
  - Catalog.API
  - Webhooks.API
  - Ordering.API

---

COMPONENT_NAME: OpenApiOptionsExtensions
TYPE: Utility
PURPOSE: Transform OpenAPI documents and operations with metadata, versioning, security, and deprecation info.
RESPONSIBILITIES:
  - Apply API title, description, and version information to OpenAPI documents
  - Append deprecation and sunset policy details to API descriptions
  - Add OAuth2 security scheme definitions (implicit flow) from Identity section configuration
  - Inject authorization responses (401 Unauthorized, 403 Forbidden) into authorized operations
  - Mark deprecated operations based on endpoint metadata
  - Add descriptive text to api-version parameters with version-specific examples
SOURCE: `src/eShop.ServiceDefaults/OpenApiOptionsExtensions.cs`
CALLS:
  - IConfiguration — to read identity and scopes
  - OpenApiOptions — to apply document and operation transformers
CALLED_BY:
  - OpenApiExtensions (via method chaining)

---

COMPONENT_NAME: ConfigurationExtensions
TYPE: Utility
PURPOSE: Provide helper method for reading required configuration values with diagnostics.
RESPONSIBILITIES:
  - Extract configuration value by key or throw InvalidOperationException with path context
  - Format error message to distinguish between root-level keys and nested section paths
SOURCE: `src/eShop.ServiceDefaults/ConfigurationExtensions.cs`
CALLS:
  - IConfiguration — to retrieve values
CALLED_BY:
  - AuthenticationExtensions (`GetRequiredValue("Url")`, `GetRequiredValue("Audience")`)
  - OpenApiExtensions (`GetRequiredValue("Document:Title")`, etc.)
  - OpenApiOptionsExtensions (via `GetRequiredSection()`)

---

COMPONENT_NAME: ClaimsPrincipalExtensions
TYPE: Utility
PURPOSE: Provide convenience methods for extracting user identity from JWT claims.
RESPONSIBILITIES:
  - Extract user ID from "sub" claim via `GetUserId()`
  - Extract user name from ClaimTypes.Name claim via `GetUserName()`
SOURCE: `src/eShop.ServiceDefaults/ClaimsPrincipalExtensions.cs`
CALLS:
  - System.Security.Claims — to access claim collections
CALLED_BY:
  - API services that need to identify current user (e.g., Basket.API, Ordering.API)

## Component Call Sequences

### Use-Case: Initialize Microservice with Service Defaults

STEP 1: WebApplication.CreateBuilder() → Program
  OPERATION: Create IHostApplicationBuilder
  PURPOSE: Bootstrap ASP.NET Core application
  SOURCE: `src/Catalog.API/Program.cs`

STEP 2: Program → ServiceDefaults.AddServiceDefaults()
  OPERATION: builder.AddServiceDefaults()
  PURPOSE: Register default services including health checks, service discovery, resilience, and OpenTelemetry
  SOURCE: `src/ServiceDefaults/Extensions.cs` (line 17)

STEP 3: ServiceDefaults → OpenTelemetry.ConfigureOpenTelemetry()
  OPERATION: builder.ConfigureOpenTelemetry()
  PURPOSE: Configure logging, metrics, and tracing with OTLP exporter support
  SOURCE: `src/ServiceDefaults/Extensions.cs` (line 47)

STEP 4: Program → ServiceDefaults.AddDefaultOpenApi()
  OPERATION: builder.AddDefaultOpenApi(apiVersioning)
  PURPOSE: Register OpenAPI document generators with versioning if OpenApi section exists
  SOURCE: `src/ServiceDefaults/OpenApi.Extensions.cs` (line 28)

STEP 5: OpenApi.Extensions → OpenApiOptionsExtensions
  OPERATION: ApplyApiVersionInfo(), ApplySecuritySchemeDefinitions(), ApplyAuthorizationChecks()
  PURPOSE: Transform documents with title, description, OAuth2 scheme, and authorization metadata
  SOURCE: `src/ServiceDefaults/OpenApiOptionsExtensions.cs`

STEP 6: Program → WebApplication.MapDefaultEndpoints()
  OPERATION: app.MapDefaultEndpoints()
  PURPOSE: Map /health and /alive diagnostic endpoints (development only)
  SOURCE: `src/ServiceDefaults/Extensions.cs` (line 102)

### Use-Case: Inter-Service HTTP Call with Authorization Token Propagation

STEP 1: ClientService → HttpClientFactory
  OPERATION: Create or retrieve HttpClient
  PURPOSE: Obtain HTTP client configured with token propagation handler
  SOURCE: Service's Program.cs (calls AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>)

STEP 2: HttpClientFactory → HttpClientAuthorizationDelegatingHandler
  OPERATION: Register delegating handler in pipeline
  PURPOSE: Intercept outbound HTTP requests to append authorization header
  SOURCE: `src/ServiceDefaults/HttpClientExtensions.cs` (line 14)

STEP 3: HttpClientAuthorizationDelegatingHandler → HttpContext
  OPERATION: context.GetTokenAsync("access_token")
  PURPOSE: Extract current request's access token
  SOURCE: `src/ServiceDefaults/HttpClientExtensions.cs` (line 35)

STEP 4: HttpClientAuthorizationDelegatingHandler → OutboundRequest
  OPERATION: request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken)
  PURPOSE: Add Bearer token to outbound request headers for downstream service
  SOURCE: `src/ServiceDefaults/HttpClientExtensions.cs` (line 38)

STEP 5: ClientService → RemoteAPI
  OPERATION: Send HTTP request with Authorization header
  PURPOSE: Invoke downstream API with user context (token-based authentication)
  SOURCE: Various service code using HttpClient

## Communication Channels

| CHANNEL_TYPE | ENDPOINT / TOPIC | SOURCE | NOTES |
|--------------|------------------|--------|-------|
| HTTP | `/alive` | `src/eShop.ServiceDefaults/Extensions.cs` | Liveness probe endpoint (development only) |
| HTTP | `/health` | `src/eShop.ServiceDefaults/Extensions.cs` | Readiness probe endpoint (development only) |
| HTTP | `/health` tag: "live" | `src/eShop.ServiceDefaults/Extensions.cs` | Liveness check using health check tag |
| HTTP | `/scalar/v1` | `src/eShop.ServiceDefaults/OpenApi.Extensions.cs` | OpenAPI UI (Scalar) — development only |
| HTTP | `/openapi/{document}.json` | `src/eShop.ServiceDefaults/OpenApi.Extensions.cs` | OpenAPI document endpoint |

## Dependency Registration and Wiring

DI_CONTAINER: Built-in ASP.NET Core IServiceCollection

### Registration File: Extensions.cs

#### Registration: AddServiceDefaults()

```csharp
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    builder.AddBasicServiceDefaults();
    builder.Services.AddServiceDiscovery();
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
    });
    return builder;
}
```

**Lifetime: Singleton** | **Scope:** Application-wide defaults

---

#### Registration: AddBasicServiceDefaults()

```csharp
public static IHostApplicationBuilder AddBasicServiceDefaults(this IHostApplicationBuilder builder)
{
    builder.AddDefaultHealthChecks();
    builder.ConfigureOpenTelemetry();
    return builder;
}
```

**Lifetime: Singleton** | **Scope:** Health checks, OpenTelemetry

---

#### Registration: AddDefaultHealthChecks()

```csharp
public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
{
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    return builder;
}
```

**Lifetime: Singleton** | **Scope:** IHealthCheckService

---

#### Registration: HttpClientAuthorizationDelegatingHandler

```csharp
builder.Services.AddHttpContextAccessor(); // Singleton
builder.Services.TryAddTransient<HttpClientAuthorizationDelegatingHandler>();
builder.AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>();
```

**Lifetime: Transient** | **Scope:** Per HTTP client request | **Implementation:** HttpMessageHandler subclass

---

#### Registration: AddDefaultAuthentication()

```csharp
services.AddAuthentication().AddJwtBearer(options =>
{
    options.Authority = identityUrl;
    options.RequireHttpsMetadata = false;
    options.Audience = audience;
    options.TokenValidationParameters.ValidIssuers = [identityUrl, ...];
});
services.AddAuthorization();
```

**Lifetime: Scoped** | **Scope:** Per request | **Implementations:** IAuthenticationService, IAuthorizationService

---

## Configuration and Secrets

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|-------------|------|-----------|----------|
| Configuration file | `Identity:Url` | NO | appsettings.json |
| Configuration file | `Identity:Audience` | NO | appsettings.json |
| Configuration file | `Identity:Scopes` | NO | appsettings.json |
| Configuration file | `OpenApi:Document:Title` | NO | appsettings.json |
| Configuration file | `OpenApi:Document:Description` | NO | appsettings.json |
| Configuration file | `OTEL_EXPORTER_OTLP_ENDPOINT` | NO | Environment variable or appsettings.json |
| Configuration file | `JWT Bearer Authority` | NO | Identity Server URL |

---

## Persistence and Data Access

DATABASE: NONE
DATA_ACCESS: NONE
MIGRATIONS_PATH: NONE
REPOSITORY_PATTERN: NO

**Notes:** eShop.ServiceDefaults is a configuration library with no direct persistence layer. Data access is delegated to consuming services (e.g., Basket.API, Ordering.API).

## Patterns and Architecture Notes

| PATTERN | EVIDENCE | SNIPPET |
|---------|----------|---------|
| **Extension Methods** | Service configuration via static extension methods on IHostApplicationBuilder and WebApplication | `AddServiceDefaults()`, `AddBasicServiceDefaults()`, `AddDefaultOpenApi()`, `MapDefaultEndpoints()` in Extensions.cs |
| **Decorator Pattern** | HttpClientAuthorizationDelegatingHandler wraps HttpMessageHandler to inject auth token | `protected override async Task<HttpResponseMessage> SendAsync()` in HttpClientExtensions.cs |
| **Document Transformer Pattern** | OpenAPI documents transformed via `IOpenApiDocumentTransformer` and `IOperationTransformer` | SecuritySchemeDefinitionsTransformer in OpenApiOptionsExtensions.cs |
| **Configuration Validation** | GetRequiredValue() throws on missing config with contextual path | `configuration[name] ?? throw new InvalidOperationException(...)` in ConfigurationExtensions.cs |
| **Facade Pattern** | Extensions provide simplified entry points to complex OpenTelemetry + service discovery + health check setup | `AddServiceDefaults()` orchestrates multiple concerns |
| **Dependency Injection (IoC)** | All configurations registered with ASP.NET Core built-in DI container | Service registration in AddServiceDefaults(), AddDefaultAuthentication(), etc. |

## Security and Operational Considerations

AUTHN_AUTHZ: JWT Bearer authentication configured via Identity section; delegated to Identity.API for token issuance and validation. File: `src/eShop.ServiceDefaults/AuthenticationExtensions.cs`

---

KNOWN_RISKS:

- **TokenValidationParameters.ValidateAudience = false** (line 45 in AuthenticationExtensions.cs) — audience validation disabled; callers must validate audience via AddJwtBearer options.Audience field
- **RequireHttpsMetadata = false** (line 41 in AuthenticationExtensions.cs) — allows non-HTTPS identity endpoint; appropriate for development but risky in production
- **Android Emulator Issuer Bypass** (line 47-51 in AuthenticationExtensions.cs) — DEBUG mode allows "https://10.0.2.2:5243" as valid issuer; remove or restrict this in production builds
- **OAuth2 Implicit Flow** (line 89 in OpenApiOptionsExtensions.cs) — OpenAPI spec uses implicit flow instead of PKCE; comment suggests future migration to authorization code flow
- **No CSRF Protection in OpenAPI UI** — Scalar UI served without CSRF tokens in development; acceptable for dev-only environment but ensure production disables OpenAPI endpoints

---

OBSERVABILITY: OpenTelemetry framework integrated for distributed tracing, metrics, and structured logging

**Logging:**
- Framework: OpenTelemetry.Logging
- Configuration: `builder.Logging.AddOpenTelemetry()` with IncludeFormattedMessage and IncludeScopes enabled
- File: `src/eShop.ServiceDefaults/Extensions.cs` (line 49)

**Metrics:**
- Instrumentation: ASP.NET Core, HTTP Client, Runtime, AI extensions
- File: `src/eShop.ServiceDefaults/Extensions.cs` (line 59)

**Tracing:**
- Instrumentation: ASP.NET Core, gRPC Client, HTTP Client, AI extensions
- Sampling: AlwaysOnSampler in development (all traces); configurable in production
- File: `src/eShop.ServiceDefaults/Extensions.cs` (line 66)

**OTLP Exporter:**
- Endpoint: `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable
- Active when endpoint is configured (non-null, non-empty)
- File: `src/eShop.ServiceDefaults/Extensions.cs` (line 84)

**Health Check Endpoints (Development Only):**
- `/alive` — Liveness probe (tagged "live" checks only)
- `/health` — Readiness probe (all checks)
- File: `src/eShop.ServiceDefaults/Extensions.cs` (line 102)

---

DEPLOYMENT: No Docker or Kubernetes artifacts in eShop.ServiceDefaults (shared library only). Consuming services (Catalog.API, Ordering.API, etc.) handle deployment.
