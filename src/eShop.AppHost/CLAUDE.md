# eShop.AppHost

## Purpose
The .NET Aspire orchestrator project that coordinates all microservices, databases, and infrastructure components. This is the **startup project** for local development.

## Key Responsibilities
- Define and configure all distributed application resources
- Set up service-to-service communication
- Configure container dependencies (Redis, RabbitMQ, PostgreSQL)
- Manage environment variables and connection strings

## Running
```powershell
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

## Configuration
- `appsettings.json` - Configure OpenAI connection strings
- `Program.cs` - Toggle `useOpenAI` or `useOllama` flags for AI features

## Dependencies
- Aspire.Hosting.RabbitMQ
- Aspire.Hosting.Redis
- Aspire.Hosting.PostgreSQL
- Aspire.Hosting.Azure.CognitiveServices
- Aspire.Hosting.Yarp
- CommunityToolkit.Aspire.Hosting.Ollama

## Key Files
- `Program.cs` - Main orchestration logic
- `Extensions.cs` - Helper extension methods
- `OpenAiExtensions.cs` - OpenAI/Ollama configuration
