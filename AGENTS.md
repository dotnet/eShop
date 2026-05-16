# eShop Project - AI Agent Instructions

## Technology Stack & Versions

This eShop reference application uses the following key technologies:

### .NET Ecosystem
- **.NET 10** - The runtime and framework for all services, APIs, and workers
- **.NET Aspire 13.1** - Orchestration and distributed application hosting for microservices
- **C#** - Primary programming language
- **ASP.NET Core** - Web framework with minimal APIs
- **Centralized Package Management** - Uses [Directory.Packages.props](Directory.Packages.props) for managed NuGet package versions across all projects with transitive pinning enabled

### Supporting Technologies
- **Entity Framework Core 10.0** - ORM for data access
- **RabbitMQ** - Message broker for event-driven architecture
- **PostgreSQL** - Primary database with pgvector for semantic search
- **Redis** - Distributed caching
- **Blazor Server** - Frontend framework
- **Duende IdentityServer 7** - Identity and authorization
- **gRPC** - High-performance inter-service communication
- **YARP** - Reverse proxy for API gateway patterns

## Architecture Overview

The eShop platform is a cloud-native microservices system with:
- **17 components**: 5 APIs, 2 workers, 2 Blazor frontends, 8 shared libraries, 1 orchestrator
- **Communication patterns**: Synchronous (HTTP REST, gRPC), Asynchronous (RabbitMQ events)
- **Key pattern**: Order processing using multi-step saga with event-driven state transitions

## When Assisting with eShop

1. Reference the current .NET 10 and .NET Aspire 13.1 compatibility requirements
2. Follow domain-driven design and microservices patterns used throughout
3. Consider both synchronous and asynchronous communication patterns
4. Review the ORDERING_DOMAIN_ANALYSIS.md and architecture.md for domain context
