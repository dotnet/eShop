# eShop Sample Reference Application - "Northern Mountains"

A reference .NET application implementing an eCommerce web site using a services-based architecture.

![eShop Reference Application architecture diagram](img/eshop_architecture.png)

![eShop homepage screenshot](img/eshop_homepage.png)


<!-- ## Build Status (GitHub Actions)

| Image | Status | Image | Status |
| ------------- | ------------- | ------------- | ------------- |
| Web Status |  [![Web Status](https://github.com/dotnet/eshop/workflows/webstatus/badge.svg?branch=dev)](https://github.com/dotnet/eshop/actions?query=workflow%3Awebstatus) | Shopping Aggregator (Web) | [![Web Shopping Aggregator](https://github.com/dotnet/eshop/workflows/webshoppingagg/badge.svg)](https://github.com/dotnet/eshop/actions?query=workflow%3Awebshoppingagg) |
| Basket API | [![Basket API](https://github.com/dotnet/eshop/workflows/basket-api/badge.svg?branch=dev)](https://github.com/dotnet/eshop/actions?query=workflow%3Abasket-api) | Shopping Aggregator (Mobile) | [![Mobile Shopping Aggregator](https://github.com/dotnet/eshop/workflows/mobileshoppingagg/badge.svg?branch=dev)](https://github.com/dotnet/eshop/actions?query=workflow%3Amobileshoppingagg) |
| Catalog API | [![Catalog API](https://github.com/dotnet/eshop/workflows/catalog-api/badge.svg)](https://github.com/dotnet/eshop/actions?query=workflow%3Acatalog-api) | Web Client (MVC) | [![WebMVC Client](https://github.com/dotnet/eshop/workflows/webmvc/badge.svg?branch=dev)](https://github.com/dotnet/eshop/actions?query=workflow%3Awebmvc) |
|Identity API | [![Identity API](https://github.com/dotnet/eshop/workflows/identity-api/badge.svg?branch=dev)](https://github.com/dotnet/eshop/actions?query=workflow%3Aidentity-api) | Web Client (SPA) | [![WebSPA Client](https://github.com/dotnet/eshop/workflows/webspa/badge.svg?branch=dev)](https://github.com/dotnet/eshop/actions?query=workflow%3Awebspa) |
| Ordering API | [![Ordering API](https://github.com/dotnet/eshop/workflows/ordering-api/badge.svg?branch=dev)](https://github.com/dotnet/eshop/actions?query=workflow%3Aordering-api) | Webhooks Client | [![Webhooks demo client](https://github.com/dotnet/eshop/workflows/webhooks-client/badge.svg)](https://github.com/dotnet/eshop/actions?query=workflow%3Awebhooks-client) |
| Payment API | [![Payment API](https://github.com/dotnet/eshop/workflows/payment-api/badge.svg?branch=dev)](https://github.com/dotnet/eshop/actions?query=workflow%3Apayment-api) | Ordering SignalR | [![Ordering SignalR](https://github.com/dotnet/eshop/workflows/ordering-signalrhub/badge.svg)](https://github.com/dotnet/eshop/actions?query=workflow%3Aordering-signalrhub) | | -->


## Getting Started

### Prerequisites

* Clone the eShop repository: https://github.com/dotnet/eshop
* (Windows only) Install Visual Studio Int Preview: https://aka.ms/vs/17/intpreview/vs_enterprise.exe
* Install & start Docker Desktop:  https://docs.docker.com/engine/install/
* Install the Azure Artifacts Credential Provider from https://github.com/microsoft/artifacts-credprovider
* Install the .NET 8 SDK version 8.0.100-rtm.23530.12 or [newer](https://github.com/dotnet/installer#table).
   * [Windows x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rtm.23530.12/dotnet-sdk-8.0.100-win-x64.exe)
   * [Linux x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rtm.23530.12/dotnet-sdk-8.0.100-linux-x64.tar.gz)
   * [macOS Arm64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rtm.23530.12/dotnet-sdk-8.0.100-osx-arm64.tar.gz)
   * [macOS x64 link](https://dotnetbuilds.azureedge.net/public/Sdk/8.0.100-rtm.23530.12/dotnet-sdk-8.0.100-osx-x64.tar.gz)
  and ensure this dotnet is on your PATH.

### Running the solution

Running eShop requires a dependency on .NET Aspire. To learn more about .NET Aspire, read [the official documentation](https://aka.ms/dotnet/aspire/docs).

```powershell
dotnet workload update --skip-sign-check --interactive
dotnet workload install aspire --skip-sign-check --interactive
dotnet restore eShop.Web.slnf
```

> [!WARNING]
> Remember to ensure that Docker is started

* (Windows only) Run the application from Visual Studio:
	* Open the `eShop.Web.slnf` file in Visual Studio
	* Ensure that `eShop.AppHost.csproj` is your startup project
	* Hit Ctrl-F5 to launch Aspire

* Or run the application from your terminal:
```powershell
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```
then look for lines like this in the console output in order to find the URL to open the Aspire dashboard:
```
Now listening on: http://localhost:18848
```

### Sample data

The sample catalog data is defined in [catalog.json](https://github.com/dotnet/eShop/blob/main/src/Catalog.API/Setup/catalog.json). Those product names, descriptions, and brand names are fictional and were generated using [GPT-35-Turbo](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/chatgpt), and the corresponding [product images](https://github.com/dotnet/eShop/tree/main/src/Catalog.API/Pics) were generated using [DALL·E 3](https://openai.com/dall-e-3).

### Contributing

For more information on contributing to this repo, please read [the contribution documentation](./CONTRIBUTING.md) and [the Code of Conduct](CODE-OF-CONDUCT.md).
