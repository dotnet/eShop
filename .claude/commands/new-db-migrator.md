---
description: Create a DB Migrator project for an existing service
argument-hint: [ServiceName]
allowed-tools: Bash, Write, Read, Glob
---

# Create DB Migrator

Create a new DB Migrator project for the **$ARGUMENTS** service.

## Prerequisites

Verify the following exist:
- `src/$ARGUMENTS.Infrastructure/$ARGUMENTSContext.cs`
- Database is configured in AppHost

## Project Structure

Create in `src/$ARGUMENTS.DbMigrator/`:

```
$ARGUMENTS.DbMigrator/
├── $ARGUMENTS.DbMigrator.csproj
├── Program.cs
├── appsettings.json
└── CLAUDE.md
```

## $ARGUMENTS.DbMigrator.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\$ARGUMENTS.Infrastructure\$ARGUMENTS.Infrastructure.csproj" />
    <ProjectReference Include="..\eShop.ServiceDefaults\eShop.ServiceDefaults.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>
</Project>
```

## Program.cs

```csharp
using eShop.$ARGUMENTS.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<$ARGUMENTSContext>("$($ARGUMENTS.ToLower())db");

var host = builder.Build();

using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<$ARGUMENTSContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting database migration...");

var pending = await context.Database.GetPendingMigrationsAsync();
if (pending.Any())
{
    logger.LogInformation("Applying {Count} migrations", pending.Count());
    await context.Database.MigrateAsync();
    logger.LogInformation("Migrations applied successfully");
}
else
{
    logger.LogInformation("No pending migrations");
}

// Add seed data logic here if needed
logger.LogInformation("Migration completed");
```

## After Creation

Remind user to:

1. Add to `eShop.AppHost.csproj`:
   ```xml
   <ProjectReference Include="..\$ARGUMENTS.DbMigrator\$ARGUMENTS.DbMigrator.csproj" />
   ```

2. Register in AppHost `Program.cs`:
   ```csharp
   builder.AddProject<Projects.$ARGUMENTS_DbMigrator>("$($ARGUMENTS.ToLower())-migrator")
       .WithReference(postgres.AddDatabase("$($ARGUMENTS.ToLower())db"))
       .WaitFor(postgres);
   ```

3. Create migrations from API project:
   ```bash
   cd src/$ARGUMENTS.API
   dotnet ef migrations add <MigrationName> -p ../$ARGUMENTS.Infrastructure -o Data/Migrations
   ```
