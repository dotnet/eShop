# eShop Java foundation

Java 21 / Spring Boot 4.1 workspace for the eShop service foundation.

## Modules

- `eshop-integration-events` — Spring-free, Jackson-compatible shared event and basket contracts. JSON property names use the PascalCase wire format expected by .NET.
- `eshop-service-defaults` — Spring Boot auto-configuration for RabbitMQ conventions, Aspire environment mapping, JWT resource-server conventions, health probes, and tracing defaults.
- `eshop-test-support` — reusable golden JSON contract assertion support.
- `eshop-foundation-smoke-app` — runnable application proving the shared modules and health endpoints compose correctly. It does not subscribe to any event queue.

## Commands

From `src-java`:

```powershell
.\mvnw.cmd verify
.\run-foundation-smoke.cmd
```

Run with development settings:

```powershell
$env:SPRING_PROFILES_ACTIVE = "dev"
.\run-foundation-smoke.cmd
```

Build and run the smoke-app container after `verify`:

```powershell
docker build -t eshop-foundation-smoke-app .\eshop-foundation-smoke-app
docker run --rm -p 8080:8080 eshop-foundation-smoke-app
```

Health probes are available at `GET /health` and `GET /alive`.

## Aspire configuration

The defaults module maps Aspire-style environment variables:

- `ConnectionStrings__eventbus` → `spring.rabbitmq.addresses`
- `ConnectionStrings__redis` → `spring.data.redis.url`
- `Identity__Url` → `spring.security.oauth2.resourceserver.jwt.issuer-uri`
- `OTEL_EXPORTER_OTLP_ENDPOINT` is consumed by Spring Boot's native
  OpenTelemetry environment mapping; exporters stay disabled when it is absent.

RabbitMQ uses the durable direct exchange `eshop_event_bus`; routing keys are event simple class names. Publishing is mandatory, persistent, and observed for W3C trace propagation; publisher returns/confirms are enabled, and listener acknowledgement defaults to manual. Applications declare their own durable queues and bindings with `EventBusConventions`; this workspace intentionally declares no subscriptions.

JWT authentication uses the `sub` claim as the principal and validates issuer and standard timestamps. Audience validation is intentionally omitted to match the eShop service convention.
