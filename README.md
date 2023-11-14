# eShop Reference Application - "Northern Mountains"

A reference .NET application implementing an eCommerce web site using a services-based architecture.

![eShop Reference Application architecture diagram](img/eshop_architecture.png)

![eShop homepage screenshot](img/eshop_homepage.png)

## Getting Started

### Prerequisites

- Clone the eShop repository: https://github.com/dotnet/eshop
- (Windows only) Install Visual Studio. Visual Studio contains tooling support for .NET Aspire that you will want to have. [Visual Studio 2022 version 17.9 Preview](https://visualstudio.microsoft.com/vs/preview/).
  - During installation, ensure that the following are selected:
    - `ASP.NET and web development` workload.
    - `.NET Aspire SDK` component in `Individual components`.
- Install & start Docker Desktop:  https://docs.docker.com/engine/install/
- Install the latest [.NET 8 SDK](https://github.com/dotnet/installer#installers-and-binaries)

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
