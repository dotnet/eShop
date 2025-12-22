---
description: Create a new backend service following Clean Architecture pattern
argument-hint: [ServiceName]
allowed-tools: Bash, Write, Read, Glob
---

# Create New Backend Service

Create a new .NET backend service named **$ARGUMENTS** following the existing Clean Architecture pattern (like Ordering service).

## Projects to Create

1. **$ARGUMENTS.API** - ASP.NET Core Minimal APIs
2. **$ARGUMENTS.Domain** - Domain entities and interfaces
3. **$ARGUMENTS.Infrastructure** - EF Core and repositories

## Reference Existing Patterns

First, read these files to understand the patterns:
- `src/Ordering.API/Program.cs`
- `src/Ordering.API/Apis/OrdersApi.cs`
- `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`
- `src/Ordering.Infrastructure/OrderingContext.cs`
- `src/Ordering.Infrastructure/Repositories/OrderRepository.cs`

## $ARGUMENTS.Domain Structure

Create in `src/$ARGUMENTS.Domain/`:

```
$ARGUMENTS.Domain/
├── $ARGUMENTS.Domain.csproj (reference MediatR)
├── GlobalUsings.cs
├── SeedWork/
│   ├── Entity.cs (base entity with domain events)
│   ├── IAggregateRoot.cs
│   ├── IRepository.cs
│   └── IUnitOfWork.cs
├── AggregatesModel/$ARGUMENTSAggregate/
│   ├── $ARGUMENTSItem.cs (sample aggregate root)
│   └── I$ARGUMENTSRepository.cs
└── CLAUDE.md
```

## $ARGUMENTS.Infrastructure Structure

Create in `src/$ARGUMENTS.Infrastructure/`:

```
$ARGUMENTS.Infrastructure/
├── $ARGUMENTS.Infrastructure.csproj (reference Domain, Npgsql.EFCore)
├── $ARGUMENTSContext.cs (DbContext implementing IUnitOfWork)
├── EntityConfigurations/
│   └── $ARGUMENTSItemEntityTypeConfiguration.cs
├── Repositories/
│   └── $ARGUMENTSRepository.cs
└── CLAUDE.md
```

## $ARGUMENTS.API Structure

Create in `src/$ARGUMENTS.API/`:

```
$ARGUMENTS.API/
├── $ARGUMENTS.API.csproj (reference Domain, Infrastructure, ServiceDefaults, EventBusRabbitMQ)
├── Program.cs (minimal, like Ordering.API)
├── GlobalUsings.cs
├── appsettings.json
├── appsettings.Development.json
├── Apis/
│   └── $ARGUMENTSApi.cs (CRUD endpoints)
├── Extensions/
│   └── Extensions.cs (AddApplicationServices)
├── Application/
│   ├── Commands/ (MediatR commands)
│   ├── Queries/ (MediatR queries)
│   └── Validations/ (FluentValidation)
└── CLAUDE.md
```

## Key Patterns to Follow

1. **Program.cs** - Keep minimal:
   ```csharp
   builder.AddServiceDefaults();
   builder.AddApplicationServices();
   builder.Services.AddProblemDetails();
   ```

2. **Minimal API** - Use typed results:
   ```csharp
   public static async Task<Results<Ok<Item>, NotFound>> GetById(...)
   ```

3. **DbContext** - Implement IUnitOfWork with transaction support

4. **Repository** - Inject DbContext, expose UnitOfWork

## After Creation

Remind user to:
1. Add project references to `eShop.AppHost.csproj`
2. Register in AppHost `Program.cs`:
   ```csharp
   var newDb = postgres.AddDatabase("$($ARGUMENTS.ToLower())db");
   builder.AddProject<Projects.$ARGUMENTS_API>("$($ARGUMENTS.ToLower())-api")
       .WithReference(rabbitMq).WaitFor(rabbitMq)
       .WithReference(newDb);
   ```
3. Create initial migration:
   ```bash
   cd src/$ARGUMENTS.API
   dotnet ef migrations add InitialCreate -p ../$ARGUMENTS.Infrastructure
   ```
