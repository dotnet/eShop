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

* Clone the eShop repository: https://github.com/dotnet-architecture/eShop
* Install Visual Studio Int Preview: https://aka.ms/vs/17/intpreview/vs_enterprise.exe
* Install & start Docker Desktop:  https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe
* Install the .NET 8 RC2 SDK version 8.0.100-rtm.23523.2 or [newer](https://github.com/dotnet/installer#table).
   1. [Windows x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rtm.23523.2/dotnet-sdk-8.0.100-rtm.23523.2-win-x64.exe)
   2. [Linux x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rtm.23523.2/dotnet-sdk-8.0.100-rtm.23523.2-linux-x64.tar.gz )
   3. [OSX x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rtm.23523.2/dotnet-sdk-8.0.100-rtm.23523.2-osx-x64.tar.gz)

### Running the solution

```powershell
dotnet workload update --skip-sign-check
dotnet workload install aspire --skip-sign-check --interactive
dotnet restore eShop.Web.slnf
```

> [!WARNING]
> Remember to ensure that Docker is started

* Run the application from Visual Studio:
	* Open the `eShop.Web.slnf` file in Visual Studio
	* Ensure that `eShop.AppHost.csproj` is your startup project
	* Hit Ctrl-F5 to launch Aspire

* To instead run the application from your terminal:

```powershell
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

### Sample data

The sample catalog data is defined in [catalog.json](https://github.com/dotnet/eShop/blob/main/src/Catalog.API/Setup/catalog.json). Those product names, descriptions, and brand names are fictional and were generated using [GPT-35-Turbo](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/chatgpt), and the corresponding [product images](https://github.com/dotnet/eShop/tree/main/src/Catalog.API/Pics) were generated using [DALL·E 3](https://openai.com/dall-e-3).

### TODO
- Contribution guide
- Architecture Overview
- Link to books
- Link to Aspire docs
