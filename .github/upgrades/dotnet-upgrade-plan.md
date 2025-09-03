# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade src\\EventBus\\EventBus.csproj
4. Upgrade src\\Ordering.Domain\\Ordering.Domain.csproj
5. Upgrade src\\IntegrationEventLogEF\\IntegrationEventLogEF.csproj
6. Upgrade src\\Ordering.Infrastructure\\Ordering.Infrastructure.csproj
7. Upgrade src\\EventBusRabbitMQ\\EventBusRabbitMQ.csproj
8. Upgrade src\\eShop.ServiceDefaults\\eShop.ServiceDefaults.csproj
9. Upgrade src\\WebAppComponents\\WebAppComponents.csproj
10. Upgrade src\\Ordering.API\\Ordering.API.csproj
11. Upgrade src\\Identity.API\\Identity.API.csproj
12. Upgrade src\\Catalog.API\\Catalog.API.csproj
13. Upgrade src\\Basket.API\\Basket.API.csproj
14. Upgrade src\\OrderProcessor\\OrderProcessor.csproj
15. Upgrade src\\PaymentProcessor\\PaymentProcessor.csproj
16. Upgrade src\\WebhookClient\\WebhookClient.csproj
17. Upgrade src\\WebApp\\WebApp.csproj
18. Upgrade src\\Webhooks.API\\Webhooks.API.csproj
19. Upgrade tests\\Ordering.UnitTests\\Ordering.UnitTests.csproj
20. Upgrade tests\\Ordering.FunctionalTests\\Ordering.FunctionalTests.csproj
21. Upgrade tests\\Catalog.FunctionalTests\\Catalog.FunctionalTests.csproj
22. Upgrade tests\\Basket.UnitTests\\Basket.UnitTests.csproj
23. Upgrade src\\eShop.AppHost\\eShop.AppHost.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                          | Current Version                | New Version                    | Description                           |
|:------------------------------------------------------|:------------------------------:|:------------------------------:|:--------------------------------------|
| Aspire.Azure.AI.OpenAI                               | 9.3.0-preview.1.25265.20      | 9.4.2-preview.1.25428.12       | Recommended for .NET 10.0            |
| Aspire.Hosting.AppHost                                | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Aspire.Hosting.Azure.CognitiveServices               | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Aspire.Hosting.PostgreSQL                            | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Aspire.Hosting.RabbitMQ                              | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Aspire.Hosting.Redis                                 | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Aspire.Npgsql                                        | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Aspire.Npgsql.EntityFrameworkCore.PostgreSQL        | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Aspire.RabbitMQ.Client                               | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Aspire.StackExchange.Redis                           | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| FluentValidation.AspNetCore                          | 11.3.0                         | 11.3.1                         | Package is deprecated                 |
| Microsoft.AspNetCore.Authentication.JwtBearer       | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.AspNetCore.Authentication.OpenIdConnect   | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.AspNetCore.Components.QuickGrid           | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.AspNetCore.Components.Web                 | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore   | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.AspNetCore.Identity.UI                    | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.AspNetCore.Mvc.Testing                    | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.AspNetCore.OpenApi                        | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.AspNetCore.TestHost                       | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.EntityFrameworkCore.Tools                 | 9.0.4                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.Extensions.ApiDescription.Server          | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.5                        | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.Extensions.Http.Resilience                | 9.5.0                          | 9.8.0                          | Recommended for .NET 10.0            |
| Microsoft.Extensions.Identity.Stores                | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.Extensions.Options                        | 9.0.5                          | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.Extensions.Options.ConfigurationExtensions | 9.0.5                         | 10.0.0-preview.7.25380.108     | Recommended for .NET 10.0            |
| Microsoft.Extensions.ServiceDiscovery               | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |
| Microsoft.Extensions.ServiceDiscovery.Yarp          | 9.3.0                          | 9.4.2                          | Recommended for .NET 10.0            |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### src\\EventBus\\EventBus.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.Extensions.DependencyInjection.Abstractions should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Options should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### src\\Ordering.Domain\\Ordering.Domain.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

Other changes:
  - System.Reflection.TypeExtensions package functionality is included with framework reference

#### src\\IntegrationEventLogEF\\IntegrationEventLogEF.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

#### src\\Ordering.Infrastructure\\Ordering.Infrastructure.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

#### src\\EventBusRabbitMQ\\EventBusRabbitMQ.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.RabbitMQ.Client should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Options.ConfigurationExtensions should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### src\\eShop.ServiceDefaults\\eShop.ServiceDefaults.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.OpenApi should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Authentication.JwtBearer should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Http.Resilience should be updated from `9.5.0` to `9.8.0` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.ServiceDiscovery should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)

#### src\\WebAppComponents\\WebAppComponents.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Components.Web should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### src\\Ordering.API\\Ordering.API.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Npgsql.EntityFrameworkCore.PostgreSQL should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `9.0.4` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - FluentValidation.AspNetCore should be updated from `11.3.0` to `11.3.1` (*package is deprecated*)

#### src\\Identity.API\\Identity.API.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Identity.UI should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `9.0.4` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Aspire.Npgsql.EntityFrameworkCore.PostgreSQL should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)

#### src\\Catalog.API\\Catalog.API.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Npgsql.EntityFrameworkCore.PostgreSQL should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `9.0.4` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.ApiDescription.Server should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Aspire.Azure.AI.OpenAI should be updated from `9.3.0-preview.1.25265.20` to `9.4.2-preview.1.25428.12` (*recommended for .NET 10.0*)

#### src\\Basket.API\\Basket.API.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.StackExchange.Redis should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)

#### src\\OrderProcessor\\OrderProcessor.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Npgsql should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)

#### src\\PaymentProcessor\\PaymentProcessor.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

#### src\\WebhookClient\\WebhookClient.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.OpenIdConnect should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Components.QuickGrid should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### src\\WebApp\\WebApp.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Azure.AI.OpenAI should be updated from `9.3.0-preview.1.25265.20` to `9.4.2-preview.1.25428.12` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.ServiceDiscovery.Yarp should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Authentication.OpenIdConnect should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### src\\Webhooks.API\\Webhooks.API.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Npgsql.EntityFrameworkCore.PostgreSQL should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `9.0.4` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### tests\\Ordering.UnitTests\\Ordering.UnitTests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

#### tests\\Ordering.FunctionalTests\\Ordering.FunctionalTests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Hosting.AppHost should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Aspire.Hosting.PostgreSQL should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Mvc.Testing should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.TestHost should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### tests\\Catalog.FunctionalTests\\Catalog.FunctionalTests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Hosting.AppHost should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Aspire.Hosting.PostgreSQL should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Mvc.Testing should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.TestHost should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### tests\\Basket.UnitTests\\Basket.UnitTests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Mvc.Testing should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Identity.Stores should be updated from `9.0.5` to `10.0.0-preview.7.25380.108` (*recommended for .NET 10.0*)

#### src\\eShop.AppHost\\eShop.AppHost.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Aspire.Hosting.AppHost should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Aspire.Hosting.RabbitMQ should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Aspire.Hosting.Redis should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Aspire.Hosting.PostgreSQL should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)
  - Aspire.Hosting.Azure.CognitiveServices should be updated from `9.3.0` to `9.4.2` (*recommended for .NET 10.0*)