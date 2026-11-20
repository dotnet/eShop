# Azure deployment

Use this reference when the user asks to deploy an Aspire app to Azure, Azure Container Apps, Azure App Service, or Azure Kubernetes Service (AKS).

## Use Aspire-native Azure deployment

For Aspire apps, start from the AppHost deployment model. Azure deployment should go through the Azure hosting integrations, AppHost environment resources, `aspire publish`, and `aspire deploy`.

Do not generate or hand-edit Azure infrastructure as the source of truth unless the user explicitly wants a published artifact handoff. The AppHost Azure environment determines what gets provisioned.

## Choose Azure target

| Target | Aspire add command | Best fit | Integration | Environment concept |
|--------|--------------------|----------|-------------|---------------------|
| Azure Container Apps | `aspire add azure-appcontainers` | Distributed/containerized services with internal/external ingress | Azure Container Apps hosting | Azure Container Apps environment |
| Azure App Service | `aspire add azure-appservice` | Public web apps/APIs that fit the App Service website model | Azure App Service hosting | Azure App Service environment |
| Azure Kubernetes Service (AKS) | `aspire add azure-kubernetes` | Kubernetes workloads where Aspire should provision Azure Kubernetes infrastructure | Azure Kubernetes hosting | Azure Kubernetes Service (AKS) environment |

If the user only says "Azure", ask which target unless the AppHost already contains exactly one Azure environment resource or the scenario strongly implies one.

Use these choices:

| Choice | Aspire add command | Use when |
|--------|--------------------|----------|
| Azure Container Apps | `aspire add azure-appcontainers` | The user wants an Azure-managed container platform for distributed apps and services. |
| Azure App Service | `aspire add azure-appservice` | The user wants Azure website hosting for web apps/APIs that fit the App Service model. |
| Azure Kubernetes Service (AKS) | `aspire add azure-kubernetes` | The user wants Aspire to provision and deploy to Azure-managed Kubernetes. |

## Docs to load

Always start with current docs:

```bash
aspire docs search "Azure Container Apps deployment"
aspire docs search "Azure App Service deployment"
aspire docs search "Azure Kubernetes Service deployment"
aspire docs search "Azure Kubernetes Service hosting integration"
aspire docs get "deploy-to-azure-container-apps"
aspire docs get "configure-azure-container-apps-environments"
aspire docs get "deploy-to-azure-app-service"
aspire docs get "set-up-azure-app-service-in-the-apphost"
aspire docs get "deploy-to-azure-kubernetes-service-aks"
aspire docs get "external-parameters"
```

Some Azure deployment searches can return noisy integration results. Search first so you can find renamed pages, then prefer the known deployment slugs above when they are available. If one of these known slugs is not available in the installed Aspire docs, inspect the closest current replacement before editing.

Use API docs before editing. Search for the target environment, endpoint, and customization concepts in the AppHost language you detected:

```bash
aspire docs api search "AddAzureContainerAppEnvironment" --language csharp
aspire docs api search "AddAzureContainerAppEnvironment" --language typescript
aspire docs api search "AddAzureAppServiceEnvironment" --language csharp
aspire docs api search "AddAzureAppServiceEnvironment" --language typescript
aspire docs api search "AddAzureKubernetesEnvironment" --language csharp
aspire docs api search "AddAzureKubernetesEnvironment" --language typescript
aspire docs api search "WithExternalHttpEndpoints" --language csharp
aspire docs api search "WithExternalHttpEndpoints" --language typescript
```

## Shared Azure preflight

Check:

- Azure CLI is installed when local deploy uses Azure CLI credentials.
- The user is authenticated (`az login`) or another `Azure:CredentialSource` is configured.
- Target subscription, location, and resource group are known.
- Required AppHost parameters are configured or can be prompted.
- The AppHost has the correct Azure target integration and environment resource.
- Production-only resources are not hidden behind run-mode-only execution context checks.
- Compute resources are assigned to the intended Azure environment when multiple compute environments exist.

Azure deployment settings:

| Setting | Environment variable | Purpose |
|---------|----------------------|---------|
| `Azure:SubscriptionId` | `Azure__SubscriptionId` | Target subscription |
| `Azure:Location` | `Azure__Location` | Azure region |
| `Azure:ResourceGroup` | `Azure__ResourceGroup` | Resource group |
| `Azure:CredentialSource` | `Azure__CredentialSource` | Credential source override |

For local development, `aspire secret set` can store these values for the AppHost:

```bash
aspire secret set "Azure:SubscriptionId" "<subscription-id>"
aspire secret set "Azure:Location" "<region>"
aspire secret set "Azure:ResourceGroup" "<resource-group>"
```

Do not use `aspire secret set` as the deployment parameter mechanism. It is a local/dev convenience today. For publish/deploy, TypeScript AppHosts, CI, and other non-interactive deploys, set deployment settings and AppHost parameters as environment variables on the `aspire deploy` process:

```bash
Azure__SubscriptionId="<subscription-id>" \
Azure__Location="westus2" \
Azure__ResourceGroup="my-app-rg" \
Parameters__api_key="<secret-value>" \
aspire deploy --apphost ./apphost.ts --environment Production --non-interactive
```

Do not print secret values. Subscription/resource group/location are not secrets, but still summarize them carefully.

If a parameter name contains `-`, use `_` in the environment variable name. For example, AppHost parameter `registry-endpoint` maps to `Parameters__registry_endpoint`.

If `az account show` reports a tenant but `aspire deploy` later prompts during `fetch-tenant`, do not assume `aspire secret set "Azure:TenantId" ...` will answer that prompt. Tenant selection can still be a pipeline prompt. Run the deploy in a real interactive terminal/PTY, or make the Azure CLI login context unambiguous before deploying, for example with `az login --tenant <tenant-id>` or `azure/login`'s `tenant-id` input in GitHub Actions.

## Azure Container Apps

Setup:

```bash
aspire add azure-appcontainers
```

Code changes:

1. Add an Azure Container Apps environment resource.

   C# AppHost shape:

   ```csharp
   var aca = builder.AddAzureContainerAppEnvironment("aca");
   ```

   TypeScript AppHost shape:

   ```typescript
   const aca = await builder.addAzureContainerAppEnvironment("aca");
   ```

2. Add `.WithExternalHttpEndpoints()` to C# compute resources that should be publicly reachable, such as projects, JavaScript/Python executables, containers, Dockerfiles, and similar workloads. For TypeScript AppHosts, use the endpoint/external endpoint API returned by docs.
3. Do not add explicit compute-environment assignment for the common single-environment case. Only if the AppHost has multiple compute environments, disambiguate each Azure Container Apps workload; in C#, add `.WithComputeEnvironment(aca)` to each compute resource that should deploy there. For TypeScript AppHosts, verify the current language-specific docs before assuming an equivalent assignment API.
4. Do not add `PublishAsAzureContainerApp(...)` / `publishAsAzureContainerApp(...)` for a default Container App deployment. Add it only when the user needs per-resource Container App customization. Use Container App Job APIs only for worker/job resources that should run as jobs.
5. Add managed Azure resources for production dependencies when appropriate, then keep normal `WithReference` / `withReference` connections so Aspire wires app settings, identities, and connection details.

Key checks:

- One external HTTP/HTTP2 target-port group becomes the main ingress.
- Multiple external endpoint groups are not supported.
- External non-HTTP endpoints are not supported.
- HTTP/HTTP2 and TCP endpoints cannot be mixed on the same target port.
- The external HTTP endpoints API marks endpoints reachable outside the Container Apps environment; verify the exact API name for C# or TypeScript before editing.
- Internal services should not be made external unless the user wants public access.
- HTTP endpoints are upgraded to HTTPS by default for deployed endpoint URLs; only disable the upgrade when the user explicitly wants HTTP.
- Volumes and bind mounts become Azure Files-backed mounts, with generated storage accounts, file shares, and managed environment storage.
- Aspire provisions or attaches ACR, builds images, pushes images, and grants pull permissions with managed identity.
- The Aspire dashboard is included by default unless the environment disables it.

Use the per-resource Container Apps customization API only when customization is required, and verify the exact API name for the AppHost language.

## Azure App Service

Setup:

```bash
aspire add azure-appservice
```

Code changes:

1. Add an Azure App Service environment resource.

   C# AppHost shape:

   ```csharp
   var appService = builder.AddAzureAppServiceEnvironment("appservice");
   ```

   TypeScript AppHost shape:

   ```typescript
   const appService = await builder.addAzureAppServiceEnvironment("appservice");
   ```

2. Add `.WithExternalHttpEndpoints()` to the C# web-facing compute resources that should become websites, such as projects, JavaScript/Python executables, containers, Dockerfiles, and similar workloads. For TypeScript AppHosts, use the endpoint/external endpoint API returned by docs.
3. Do not add explicit compute-environment assignment for the common single-environment case. Only if the AppHost has multiple compute environments, disambiguate each App Service workload; in C#, add `.WithComputeEnvironment(appService)` to each website compute resource that should deploy there.
4. Keep background workers, infrastructure containers, and internal-only services out of App Service unless the docs say the resource is supported. Move dependencies to managed Azure resources or choose Container Apps/Azure Kubernetes Service (AKS).
5. Do not add `PublishAsAzureAppServiceWebsite(...)` / `publishAsAzureAppServiceWebsite(...)` for a default website deployment. Add it only when the user needs website customization such as app settings, deployment slots, tags, or infrastructure callbacks.
6. Use `SkipEnvironmentVariableNameChecks()` only when the user intentionally accepts App Service's dashed-setting behavior and the app does not depend on the original dashed key at runtime.

Key checks:

- App Service is a public website model.
- Supported workloads are web-facing project resources and Dockerfile-backed web containers.
- External HTTP/HTTPS endpoints are required for deployed websites.
- Only HTTP/HTTPS endpoints are supported.
- App Service supports a single target port for a deployed website. Multi-port public apps do not fit this target.
- Internal-only endpoints and arbitrary infrastructure containers do not fit this target.
- App Service upgrades external HTTP endpoint URLs to HTTPS by default.
- The environment creates an App Service Plan with default SKU `P0V3`/Premium Linux, a container registry, managed identity, and the dashboard by default.
- Application Insights is optional and must be enabled/configured intentionally.
- Environment variable names containing `-` fail validation because App Service removes dashes at runtime; prefer dash-free connection names rather than bypassing validation.
- Use managed Azure resources for databases, caches, and brokers.

Use the per-site App Service customization API only for app settings, deployment slots, tags, validation overrides, or other per-site changes, and verify the exact API name for the AppHost language.

## Azure Kubernetes Service (AKS)

Setup:

```bash
aspire add azure-kubernetes
```

Code changes:

1. Add an Azure Kubernetes Service (AKS) environment resource.

   C# AppHost shape:

   ```csharp
   var aks = builder.AddAzureKubernetesEnvironment("aks");
   ```

   TypeScript AppHost shape:

   ```typescript
   const aks = await builder.addAzureKubernetesEnvironment("aks");
   ```

2. Do not add explicit compute-environment assignment for the common single-environment case. Only if the AppHost has multiple compute environments, disambiguate each Azure Kubernetes Service (AKS) workload; in C#, add `.WithComputeEnvironment(aks)` to each compute resource that should deploy there, such as projects, JavaScript/Python executables, containers, Dockerfiles, and similar workloads.
3. Do not add a separate plain Kubernetes environment for the same Azure Kubernetes Service (AKS) target. The Azure Kubernetes environment owns the Kubernetes/Helm deployment path.
4. Use Azure Kubernetes Service (AKS) customization APIs only when needed, such as node pools, system node pool SKU/count, subnet integration, workload identity, custom ACR, or Application Gateway for Containers.
5. Use Kubernetes Gateway/Ingress/service customization APIs for public exposure. Do not assume adding the environment makes every workload public.

For Azure Kubernetes Service (AKS) details, also load [kubernetes.md](kubernetes.md).

Azure Kubernetes Service (AKS) uses the Azure Kubernetes integration to provision Azure Kubernetes Service (AKS) infrastructure and then deploys through an inner Kubernetes/Helm environment. It auto-creates ACR by default, supports explicit ACR replacement, and can customize node pools, subnets, workload identity, and Application Gateway for Containers. Verify exact APIs before editing.

## Preview and deploy

List pipeline steps:

```bash
aspire publish --list-steps
aspire deploy --list-steps
```

Generate artifacts without applying:

```bash
aspire publish -o azure-artifacts
```

Deploy:

```bash
aspire deploy
```

For local Azure deploys, prefer a real interactive terminal for the first apply. Azure deployment can prompt for values that are not AppHost parameters, such as tenant selection when multiple tenants are available. Use `--non-interactive` only after configuring deploy-time values with environment variables and confirming the Azure CLI login context is unambiguous.

Do not pipe an interactive `aspire deploy` through `tee`, `tail`, or another command when prompts may appear. The pipe can make the current terminal non-interactive and selection prompts will fail. Use the attached terminal output and the Aspire CLI log path printed by the command instead of capturing the transcript with a pipe.

Use `--environment <name>` for staging/production context:

```bash
aspire deploy --environment staging
```

Published Azure artifacts are for preview or handoff. `aspire deploy` resolves parameters and applies the Azure deployment from the AppHost model.

## Destroy

Use Aspire to run the Azure target's destroy pipeline:

```bash
aspire destroy --environment <name>
```

For Azure, `aspire destroy` delegates to the Azure deployment target for the selected AppHost/environment. Confirm the Azure subscription, resource group, environment name, and AppHost before running destroy. Use `--yes` only after the user has explicitly approved teardown or in an environment-protected cleanup workflow. Use Azure CLI after destroy to verify resource removal or investigate leftovers; do not start with `az group delete` or target-specific delete commands for resources managed by the Aspire Azure target unless `aspire destroy` cannot complete.

## Common troubleshooting

Use Azure CLI to inspect live Azure state that Aspire deployed, while keeping deployment changes in the AppHost and `aspire deploy`.

Start by confirming the active subscription and resource group:

```bash
az account show --query "{name:name, id:id, tenantId:tenantId}" --output table
az group show --name "<resource-group>" --query "{name:name, location:location, provisioningState:properties.provisioningState}" --output table
az deployment group list --resource-group "<resource-group>" --query "[].{name:name, state:properties.provisioningState, timestamp:properties.timestamp}" --output table
az resource list --resource-group "<resource-group>" --query "[].{name:name, type:type, location:location}" --output table
```

Use target-specific inspection commands:

```bash
# Azure Container Apps
az containerapp list --resource-group "<resource-group>" --query "[].{name:name, state:properties.runningStatus, fqdn:properties.configuration.ingress.fqdn}" --output table
az containerapp revision list --resource-group "<resource-group>" --name "<container-app-name>" --query "[].{name:name, active:properties.active, healthState:properties.healthState}" --output table
az containerapp logs show --resource-group "<resource-group>" --name "<container-app-name>" --tail 100

# Azure App Service
az webapp list --resource-group "<resource-group>" --query "[].{name:name, state:state, hostNames:defaultHostName}" --output table
az webapp config appsettings list --resource-group "<resource-group>" --name "<web-app-name>" --query "[].name" --output table
az webapp log tail --resource-group "<resource-group>" --name "<web-app-name>"

# Azure Kubernetes Service (AKS)
az aks show --resource-group "<resource-group>" --name "<cluster-name>" --query "{name:name, state:provisioningState, fqdn:fqdn, kubernetesVersion:kubernetesVersion}" --output table
az aks get-credentials --resource-group "<resource-group>" --name "<cluster-name>"
kubectl get pods,svc,ingress --all-namespaces
helm list --all-namespaces
```

When troubleshooting generated Azure resources, match the live resource names and tags back to the Aspire deployment summary, AppHost environment resource name, and selected `--environment` value. Do not print secret values; for Key Vault or app settings, inspect key names and references rather than values.

- Missing Azure settings: configure `Azure__SubscriptionId`, `Azure__Location`, and optionally `Azure__ResourceGroup` on the deploy process.
- Wrong subscription: check `az account show` and compare to `Azure__SubscriptionId` or AppHost config.
- Failed resource group deployment: inspect the failed deployment with `az deployment group show --resource-group "<resource-group>" --name "<deployment-name>"` and then inspect operation errors with `az deployment operation group list --resource-group "<resource-group>" --name "<deployment-name>" --query "[?properties.provisioningState=='Failed']"`.
- Parameter prompts in CI: provide `Parameters__*` environment variables through pipeline secrets/variables.
- Secrets to Key Vault: use the Azure Key Vault hosting integration and secret APIs only after confirming docs and user intent.
- Public endpoint surprise: inspect external endpoint configuration and target endpoint rules before deployment.
- Container Apps revision unhealthy: check revision health, ingress, image pull, managed identity, and logs with `az containerapp revision list` and `az containerapp logs show`.
- App Service unsupported resource: move backing services to managed Azure resources or choose Container Apps/Azure Kubernetes Service (AKS) instead.
- App Service dashed setting failure: rename the connection/environment key if possible; bypass validation only when the app does not depend on the original dashed key at runtime.
- Azure Kubernetes Service (AKS) workload not reachable: refresh credentials with `az aks get-credentials`, then inspect pods, services, ingress/gateway resources, and Helm release status with `kubectl` and `helm`.
