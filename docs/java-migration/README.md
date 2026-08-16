# Java microservice migration

The Java workspace lives in `src-java` and targets Java 21. Use the checked-in
Maven Wrapper so contributors do not need a machine-wide Maven installation.

```powershell
cd src-java
.\mvnw.cmd verify
```

## Foundation modules

- `eshop-integration-events` owns transport DTOs and PascalCase JSON behavior.
- `eshop-service-defaults` owns Aspire configuration mapping, RabbitMQ
  conventions, JWT resource-server defaults, health endpoints, and telemetry
  defaults.
- `eshop-test-support` provides reusable compatibility-test helpers.
- `eshop-foundation-smoke-app` proves that Aspire can launch a Spring Boot
  process without replacing a production service.

Service modules may depend on foundation modules, but foundation modules must
not depend on a service.

## Aspire shadow registration

The smoke app is disabled by default and uses the unique resource name
`java-foundation-smoke`. It is not connected to YARP or the WebApp and it does
not subscribe to a production RabbitMQ queue.

Enable it for a local interoperability check:

```powershell
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj -- `
  --JavaServices:FoundationSmokeEnabled=true
```

The existing .NET services remain active. Open the Aspire dashboard and verify
that `java-foundation-smoke` reaches a healthy state.

## Aspire configuration mapping

Aspire injects .NET-style environment names. The shared Java configuration
maps these without changing AppHost resource names:

- `ConnectionStrings__eventbus` to the Spring RabbitMQ connection.
- `ConnectionStrings__redis` to the Spring Redis connection.
- `ConnectionStrings__catalogdb`, `ConnectionStrings__orderingdb`, and
  `ConnectionStrings__webhooksdb` to the applicable service datasource.
- `Identity__Url` to the resource-server issuer.
- `OTEL_EXPORTER_OTLP_ENDPOINT` to the OpenTelemetry exporter.

## Future service cutover

Each service gets one opt-in AppHost switch when its port is complete. The
switch must create exactly one resource with the existing logical name, such as
`catalog-api`; never register .NET and Java implementations under that name at
the same time.

Cutover sequence:

1. Verify unit, integration, API, data, and mixed-runtime event contracts.
2. Stop the .NET state-changing consumer.
3. Enable the Java resource under the existing logical name.
4. Run existing UI and saga smoke tests.
5. Observe health, traces, and queue behavior.
6. Roll back by restoring the .NET resource if a gate fails.

Canonical compatibility fixtures and the full acceptance gate are documented
in `tests/Contracts/README.md`.
