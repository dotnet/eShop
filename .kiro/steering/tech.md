# Technology Stack & Build System

## Core Technologies
- **.NET 10**: Latest .NET version with C# 12
- **.NET Aspire**: Cloud-native orchestration and service discovery
- **ASP.NET Core**: Web APIs and web applications
- **Entity Framework Core**: ORM with PostgreSQL provider
- **gRPC**: High-performance RPC communication
- **MediatR**: CQRS and mediator pattern implementation

## Infrastructure & Data
- **PostgreSQL**: Primary database with pgvector extension for AI/ML
- **Redis**: Caching and session storage
- **RabbitMQ**: Message broker for event-driven communication
- **Docker**: Containerization for all services

## Authentication & Security
- **Duende IdentityServer**: OAuth 2.0/OpenID Connect provider
- **JWT Bearer**: API authentication
- **ASP.NET Core Identity**: User management

## AI & Machine Learning
- **Azure OpenAI**: Optional AI integration
- **Ollama**: Local AI model hosting (optional)
- **pgvector**: Vector database capabilities

## Frontend Technologies
- **Blazor**: Server-side web UI framework
- **.NET MAUI**: Cross-platform mobile and desktop apps
- **Blazor Hybrid**: Web technologies in native apps

## Testing & Quality
- **MSTest**: Primary testing framework with MSTest.Sdk
- **Playwright**: End-to-end browser testing
- **NSubstitute**: Mocking framework
- **Microsoft.Testing.Platform**: Test runner

## Package Management
- **Central Package Management**: All package versions managed in `Directory.Packages.props`
- **NuGet**: Package manager with custom `nuget.config`

## Common Commands

### Development
```bash
# Run the entire application with Aspire orchestration
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Build the entire solution
dotnet build

# Restore packages
dotnet restore

# Run tests
dotnet test

# Run specific project
dotnet run --project src/WebApp/WebApp.csproj
```

### Docker & Containers
```bash
# Ensure Docker Desktop is running before starting the application
# All services are containerized and orchestrated through Aspire
```

### Testing
```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/

# Run Playwright E2E tests
npx playwright test

# Install Playwright browsers
npx playwright install
```

### Database
```bash
# EF migrations are handled automatically by Aspire orchestration
# Database initialization happens on service startup
```

## Development Environment Setup

### Prerequisites
- **.NET 10 SDK** (specified in global.json)
- **Docker Desktop** (required for infrastructure services)
- **Visual Studio 2022 17.10+** or **VS Code with C# Dev Kit**

### Optional Tools
- **Visual Studio Code**: Alternative IDE
- **.NET MAUI Workload**: For mobile app development
- **Azure CLI**: For Azure deployment with `azd`

## Configuration Patterns
- **appsettings.json**: Environment-specific configuration
- **Service Discovery**: Automatic service resolution via Aspire
- **Health Checks**: Built-in health monitoring endpoints
- **OpenTelemetry**: Distributed tracing and metrics
- **Resilience**: Built-in retry policies and circuit breakers

## Code Style
- **EditorConfig**: Enforced code formatting rules
- **4-space indentation** for C# files
- **2-space indentation** for XML/config files
- **UTF-8 BOM** encoding for code files
- **PascalCase** for constants and public members
- **var** keyword preferred when type is apparent