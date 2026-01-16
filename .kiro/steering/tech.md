# Technology Stack

## Core Framework

- **.NET 10** (SDK 10.0.100) - Latest .NET version with preview features enabled
- **C# 13** with implicit usings enabled
- **.NET Aspire 13.1.0** - Cloud-native orchestration and service discovery

## Build System

- **MSBuild** with centralized package management
- **Artifacts output** - Build outputs go to `artifacts/` directory
- **Solution files**:
  - `eShop.slnx` - Full solution
  - `eShop.Web.slnf` - Web-focused solution filter

## Key Technologies

### Infrastructure & Hosting
- **Docker** - Required for running dependencies
- **PostgreSQL** (with pgvector) - Primary database
- **Redis** - Caching and session storage
- **RabbitMQ** - Message bus for event-driven communication

### Web & API
- **ASP.NET Core 10.0** - Web framework
- **Minimal APIs** - Endpoint definitions
- **gRPC** (2.71.0) - Inter-service communication
- **YARP** - Reverse proxy for BFF pattern
- **API Versioning** (8.1.0) - Endpoint versioning
- **Scalar** - OpenAPI documentation UI

### Authentication & Authorization
- **Duende IdentityServer 7.3** - OAuth2/OIDC provider
- **JWT Bearer Authentication**
- **ASP.NET Core Identity**

### Data Access
- **Entity Framework Core 10.0**
- **Npgsql** - PostgreSQL provider
- **Dapper** - Lightweight ORM for queries
- **Pgvector** - Vector similarity search

### Messaging & Events
- **RabbitMQ Client** via Aspire
- **MediatR 13.0** - In-process messaging
- **Custom EventBus** - Integration event abstraction

### Observability
- **OpenTelemetry** - Distributed tracing and metrics
- **Health Checks** - Service health monitoring
- **Aspire Dashboard** - Development-time observability

### Testing
- **MSTest 4.0** - Test framework
- **xUnit v3** - Alternative test framework
- **Playwright** - E2E browser testing
- **NSubstitute** - Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing

### AI (Optional)
- **Azure OpenAI** - AI capabilities (opt-in)
- **Ollama** - Local LLM support (opt-in)

## Common Commands

### Running the Application

```bash
# Run with .NET CLI (recommended)
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Trust HTTPS certificates (first time only)
dotnet dev-certs https --trust
```

### Building

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Catalog.API/Catalog.API.csproj

# Build in Release mode
dotnet build -c Release
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run E2E tests (Playwright)
npm run test:e2e
```

### Database Migrations

```bash
# Add migration (example for Catalog)
dotnet ef migrations add MigrationName --project src/Catalog.API

# Update database
dotnet ef database update --project src/Catalog.API
```

### Docker

```bash
# Ensure Docker Desktop is running
# The application will automatically start required containers via Aspire
```

### Azure Deployment

```bash
# Initialize Azure Developer CLI
azd init

# Login to Azure
azd auth login

# Deploy to Azure
azd up
```

## Development Tools

### Required
- **Docker Desktop** - Container runtime
- **.NET 10 SDK** - Development SDK

### Recommended
- **Visual Studio 2022 17.10+** with:
  - ASP.NET and web development workload
  - .NET Aspire SDK component
  - Optional: .NET MAUI workload for mobile apps
- **Visual Studio Code** with C# Dev Kit (alternative)

## Configuration

### Central Package Management
All package versions are managed in `Directory.Packages.props` with `ManagePackageVersionsCentrally` enabled.

### Build Properties
- `TreatWarningsAsErrors`: true
- `ImplicitUsings`: enabled
- `DebugType`: embedded
- `UseArtifactsOutput`: true (outputs to `artifacts/`)

### Code Style
Enforced via `.editorconfig`:
- Indent: 4 spaces for C#, 2 spaces for XML
- Charset: UTF-8 with BOM
- New line before braces (Allman style)
- `var` preferred for built-in types

## Environment Variables

### For Testing
- `ESHOP_USE_HTTP_ENDPOINTS=1` - Force HTTP endpoints (CI/testing)

### For AI Features
Configure in `src/eShop.AppHost/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "OpenAi": "Endpoint=xxx;Key=xxx;"
  }
}
```

## Launch Profiles

Projects use launch profiles defined in `Properties/launchSettings.json`:
- `http` - HTTP-only endpoints
- `https` - HTTPS endpoints (default)

The AppHost determines which profile to use based on configuration.
