# eShop Reference Application - "AdventureWorks"

A reference .NET application implementing an e-commerce website using a services-based architecture with [Aspire](https://aspire.dev/).

![eShop Reference Application architecture diagram](img/eshop_architecture.png)

![eShop homepage screenshot](img/eshop_homepage.png)

## Getting Started

This version of eShop is based on .NET 10.

Previous eShop versions:
* [.NET 8](https://github.com/dotnet/eShop/tree/release/8.0)

### Prerequisites

- Clone the eShop repository: https://github.com/dotnet/eshop
- Install and start a supported OCI-compatible container runtime, such as [Docker Desktop](https://docs.docker.com/engine/install/) or [Podman](https://podman.io/)

#### Windows with Visual Studio
- Install [Visual Studio 2022 version 17.10 or newer](https://visualstudio.microsoft.com/vs/).
    - Select the following workloads:
        - `ASP.NET and web development` workload.
        - `Aspire SDK` component in `Individual components`.
        - Optional: `.NET Multi-platform App UI development` to run client apps

Or

- Run the following commands in an elevated PowerShell terminal to automatically configure your environment with the required tools to build and run this application. (A restart is required and included in the script below.)

```powershell
install-Module -Name Microsoft.WinGet.Configuration -AllowPrerelease -AcceptLicense -Force
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
Get-WinGetConfiguration -File .\.config\configuration.vs.winget | Invoke-WinGetConfiguration -AcceptConfigurationAgreements
```

Or

- From Dev Home go to `Machine Configuration -> Clone repositories`. Enter the URL for this repository. In the confirmation screen look for the section `Configuration File Detected` and click `Run File`.

#### Mac, Linux, & Windows without Visual Studio
- Install the latest [.NET 10 SDK](https://dot.net/download?cid=eshop)

Or

- On Windows, run the following commands in an elevated PowerShell terminal to automatically configure your environment with the required tools to build and run this application. (A restart is required after running the script below.)

##### Install Visual Studio Code and related extensions
```powershell
install-Module -Name Microsoft.WinGet.Configuration -AllowPrerelease -AcceptLicense  -Force
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
Get-WinGetConfiguration -File .\.config\configuration.vsCode.winget | Invoke-WinGetConfiguration -AcceptConfigurationAgreements
```

> Note: These commands may require `sudo`

- Optional: Install [Visual Studio Code with C# Dev Kit](https://code.visualstudio.com/docs/csharp/get-started)
- Optional: Install [.NET MAUI Workload](https://learn.microsoft.com/dotnet/maui/get-started/installation?tabs=visual-studio-code)

> Note: When running on Mac with Apple Silicon (M series processor), Rosetta 2 for grpc-tools. 

### Running the solution

> [!WARNING]
> Remember to ensure that your container runtime is started

- (Windows only) Run the application from Visual Studio:
- Open the `eShop.Web.slnf` file in Visual Studio
- Ensure that `eShop.AppHost.csproj` is your startup project
- Hit Ctrl-F5 to launch Aspire

* Or run the application from your terminal:
```powershell
aspire run
```
`aspire.config.json` points this command to `src/eShop.AppHost/eShop.AppHost.csproj`. This repo also includes test AppHosts, so use `--apphost <path>` when you want to target one explicitly. Then look for lines like this in the console output to find the URL to open the Aspire dashboard:
```sh
Login to the dashboard at: http://localhost:19888/login?t=uniquelogincodeforyou
```

### Running tests

Run the server tests:

```powershell
dotnet test --solution eShop.Web.slnf
```

Run the Playwright browser journeys. Playwright starts the AppHost automatically, so ensure your container runtime is running first.

```powershell
npm ci
npx playwright install chromium
npm run test:e2e
```

### Optional: AI Chatbot with Microsoft Foundry

To use Microsoft Foundry for chat and embeddings, set `UseFoundry=true` in the
AppHost environment. Aspire provisions the Foundry resource and the
`gpt-4.1-mini` and `text-embedding-3-small` deployments, then injects their
connection information into the consuming projects.

The Foundry hosting integration currently uses a preview package. It replaces
the sample's previous direct OpenAI and existing Azure OpenAI configuration paths.

```powershell
$env:UseFoundry = "true"
aspire run
```

See the [Microsoft Foundry Aspire hosting integration](https://aspire.dev/integrations/cloud/azure-ai-foundry/) for configuration and deployment details.

### Deploy with Aspire CLI

Use Aspire deployment from the AppHost model.

This sample intentionally deploys disposable PostgreSQL, Redis, and RabbitMQ containers to Azure Container Apps. It is suitable for evaluation and demonstrations, not production data.

Prerequisites:
- A supported container runtime must be running.
- Azure CLI must be authenticated.
- Azure deployment settings must be set (`Azure__SubscriptionId`, `Azure__Location`, `Azure__ResourceGroup`).

1. Preview deployment steps:
```sh
aspire publish --list-steps
aspire deploy --list-steps
```
2. Publish deployment artifacts for inspection or handoff:
```sh
aspire publish
```
3. Deploy directly from the AppHost model:
```sh
aspire deploy
```

Example (PowerShell):
```powershell
$env:Azure__SubscriptionId = "<subscription-id>"
$env:Azure__Location = "eastus"
$env:Azure__ResourceGroup = "rg-eshop-prod"
aspire deploy --non-interactive
```

`aspire deploy` evaluates the AppHost directly; it does not consume a previous `aspire publish` output directory. Omit `--non-interactive` for interactive use; add it for automation or agent-driven runs so Aspire does not prompt for missing deployment settings. Set the required values explicitly.

## Contributing

For more information on contributing to this repo, read [the contribution documentation](./CONTRIBUTING.md) and [the Code of Conduct](CODE-OF-CONDUCT.md).

### Sample data

The sample catalog data is defined in [catalog.json](https://github.com/dotnet/eShop/blob/main/src/Catalog.API/Setup/catalog.json). Those product names, descriptions, and brand names are fictional and were generated using [GPT-35-Turbo](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/chatgpt), and the corresponding [product images](https://github.com/dotnet/eShop/tree/main/src/Catalog.API/Pics) were generated using [DALL·E 3](https://openai.com/dall-e-3).

## eShop on Azure

For a version of this app configured for deployment on Azure, please view [the eShop on Azure](https://github.com/Azure-Samples/eShopOnAzure) repo.
