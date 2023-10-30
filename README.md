# eShop Sample Reference Application - "Northern Mountains"

A reference .NET application implementing an eCommerce web site using a services-based architecture.

![eShop Reference Application architecture diagram](img/eshop_architecture.png)

![eShop homepage screenshot](img/eshop_homepage.png)


<!-- ## Build Status (GitHub Actions)

| Image | Status | Image | Status |
| ------------- | ------------- | ------------- | ------------- |
| Web Status |  [![Web Status](https://github.com/dotnet-architecture/eshop/workflows/webstatus/badge.svg?branch=dev)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Awebstatus) | Shopping Aggregator (Web) | [![Web Shopping Aggregator](https://github.com/dotnet-architecture/eshop/workflows/webshoppingagg/badge.svg)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Awebshoppingagg) |
| Basket API | [![Basket API](https://github.com/dotnet-architecture/eshop/workflows/basket-api/badge.svg?branch=dev)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Abasket-api) | Shopping Aggregator (Mobile) | [![Mobile Shopping Aggregator](https://github.com/dotnet-architecture/eshop/workflows/mobileshoppingagg/badge.svg?branch=dev)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Amobileshoppingagg) |
| Catalog API | [![Catalog API](https://github.com/dotnet-architecture/eshop/workflows/catalog-api/badge.svg)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Acatalog-api) | Web Client (MVC) | [![WebMVC Client](https://github.com/dotnet-architecture/eshop/workflows/webmvc/badge.svg?branch=dev)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Awebmvc) |
|Identity API | [![Identity API](https://github.com/dotnet-architecture/eshop/workflows/identity-api/badge.svg?branch=dev)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Aidentity-api) | Web Client (SPA) | [![WebSPA Client](https://github.com/dotnet-architecture/eshop/workflows/webspa/badge.svg?branch=dev)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Awebspa) |
| Ordering API | [![Ordering API](https://github.com/dotnet-architecture/eshop/workflows/ordering-api/badge.svg?branch=dev)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Aordering-api) | Webhooks Client | [![Webhooks demo client](https://github.com/dotnet-architecture/eshop/workflows/webhooks-client/badge.svg)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Awebhooks-client) |
| Payment API | [![Payment API](https://github.com/dotnet-architecture/eshop/workflows/payment-api/badge.svg?branch=dev)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Apayment-api) | Ordering SignalR | [![Ordering SignalR](https://github.com/dotnet-architecture/eshop/workflows/ordering-signalrhub/badge.svg)](https://github.com/dotnet-architecture/eshop/actions?query=workflow%3Aordering-signalrhub) | | -->


## Getting Started

### Prerequisites

* Install Visual Studio 2022 Int Preview: https://aka.ms/vs/17/intpreview/vs_enterprise.exe
* Install and start Docker Desktop:  https://docs.docker.com/desktop/
* Install the Azure Artifacts Credential Provider from https://github.com/microsoft/artifacts-credprovider
* Install the .NET 8 SDK (RC2 or newer).
   1. [Windows x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rc.2.23472.8/dotnet-sdk-8.0.100-rc.2.23472.8-win-x64.exe)
   2. [Linux x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rc.2.23472.8/dotnet-sdk-8.0.100-rc.2.23472.8-linux-x64.tar.gz)
   3. [OSX x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rc.2.23472.8/dotnet-sdk-8.0.100-rc.2.23472.8-osx-x64.tar.gz)

### Running the solution
* Clone the eShop repository: https://github.com/dotnet-architecture/eShop
* Open a terminal and navigate to the repository's `src` directory (where the NuGet.config file resides)
* Update .NET workloads

```powershell
dotnet workload update --skip-sign-check
```
* Install Aspire .NET workload
```powershell
dotnet workload install aspire --skip-sign-check --interactive
```
* To run the application from Visual Studio:
   * Open the eShop.sln file in Visual Studio
   * Set eShop.AppHost.csproj as your startup project
   * Hit F5 to debug the solution, or Ctrl+F5 to run without debugging

* To instead run the application from your terminal:
```powershell
dotnet run --project eShop.AppHost/eShop.AppHost.csproj
```

### TODO
- Contribution guide
- Architecture Overview
- Link to books
- Link to Aspire docs
