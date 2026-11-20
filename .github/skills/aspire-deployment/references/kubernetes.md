# Kubernetes and Azure Kubernetes Service (AKS) deployment

Use this reference when the user asks for Kubernetes, Helm, kubectl, Azure Kubernetes Service (AKS), or cluster deployment.

## Choose Kubernetes vs Azure Kubernetes Service (AKS)

Aspire has two Kubernetes paths:

| Target | Use when | Integration | Environment concept |
|--------|----------|-------------|---------------------|
| Existing or externally-managed Kubernetes cluster | The cluster already exists, or the user explicitly wants a provider-managed cluster outside Aspire such as DigitalOcean Kubernetes (DOKS). Current `kubectl` context should point at the target cluster. | Kubernetes hosting | Kubernetes environment |
| Azure Kubernetes Service (AKS) | Aspire should provision Azure Kubernetes Service (AKS), ACR, identity, and Azure dependencies | Azure Kubernetes hosting | Azure Kubernetes Service (AKS) environment |

If the user says "Kubernetes" but not "Azure Kubernetes Service (AKS)", ask whether they want an existing/external cluster or a new Azure Kubernetes Service (AKS) cluster.

If the user names a non-Azure Kubernetes provider, such as DigitalOcean Kubernetes, use the existing/external Kubernetes path. Aspire will deploy into that cluster with Helm, but it does not own the provider's cluster or registry lifecycle unless a target-specific Aspire integration exists.

If the user says "Azure Kubernetes Service (AKS)", do not ask the existing-cluster question; use the Azure Kubernetes Service (AKS) path and also load the Azure reference for shared Azure settings.

## Docs to load

Always start with current docs:

```bash
aspire docs search "Kubernetes deployment"
aspire docs search "Kubernetes hosting integration"
aspire docs get "deploy-to-kubernetes-clusters"
aspire docs get "kubernetes-integration"
aspire docs get "deploy-to-azure-kubernetes-service-aks"
aspire docs get "azure-kubernetes-service-aks-integration"
aspire docs search "Azure Kubernetes Service hosting integration"
aspire docs get "<slug>"
```

Use API docs before editing. Search in the AppHost language you detected:

```bash
aspire docs api search "Kubernetes environment" --language csharp
aspire docs api search "Kubernetes environment" --language typescript
aspire docs api search "Helm" --language csharp
aspire docs api search "Helm" --language typescript
aspire docs api search "Kubernetes service customization" --language csharp
aspire docs api search "Kubernetes service customization" --language typescript
```

## Existing or external Kubernetes cluster setup

Expected package and AppHost environment:

```bash
aspire add kubernetes
```

Add a Kubernetes environment resource using the C# or TypeScript API shape returned by Aspire docs.

For vanilla Kubernetes and externally-managed clusters, a container registry is required for project/container image deployment because Aspire has no local-registry fallback for Kubernetes. Cluster nodes must pull the built images.

Add a container registry resource to the AppHost using the language-specific API shape returned by Aspire docs. Aspire can use a single registry as the default target; use per-resource registry assignment only when different workloads must use different registries.

Verify the registry is reachable from both the agent machine and cluster nodes.

When the user asks to create a provider-managed cluster or registry outside Aspire, such as a DigitalOcean Kubernetes cluster and DigitalOcean Container Registry, confirm the billable resource choice first. After creation, configure `kubectl`, registry authentication, and any provider-specific registry-to-cluster integration before running `aspire deploy`.

### Existing/external cluster code changes

Make these changes in the AppHost:

1. Run `aspire add kubernetes` if the AppHost does not already reference the Kubernetes hosting integration.
2. Add a Kubernetes environment resource.

   C# AppHost shape:

   ```csharp
   var k8s = builder.AddKubernetesEnvironment("k8s");
   ```

   TypeScript AppHost shape:

   ```typescript
   const k8s = await builder.addKubernetesEnvironment("k8s");
   ```

3. Add a container registry for project/container images, for example `builder.AddContainerRegistry(...)` in C# or the TypeScript equivalent returned by API docs. If one registry exists, Aspire can use it as the default target; use `WithContainerRegistry(...)` / `withContainerRegistry(...)` only when a specific resource should use a specific registry.
4. Do not add explicit compute-environment assignment for the common single-environment case. Only if the AppHost has multiple compute environments, disambiguate each Kubernetes workload; in C#, add `.WithComputeEnvironment(k8s)` to each compute resource that should deploy to this cluster.
5. For TypeScript AppHosts, verify the current language-specific docs before assuming an equivalent assignment API.
6. Use `k8s.WithHelm(...)` / `k8s.withHelm(...)` only when the user needs chart name, release name, namespace, chart version, or other Helm settings.
7. Use `k8s.AddGateway(...)`, `k8s.AddIngress(...)`, or the TypeScript equivalents when public exposure is required. Otherwise services remain internal by default. For a simple public web frontend on a cloud Kubernetes provider, a per-resource `LoadBalancer` Service can be the direct exposure model; keep backend/internal services as `ClusterIP`.
   - **Routed endpoints must be marked external.** Any endpoint exposed by an Ingress (`WithPath(...)`) or a Gateway (`WithRoute(...)`), or wired up via `WithDefaultBackend(...)`, must come from a resource that opts in with `.WithExternalHttpEndpoints()` (C#) or `isExternal: true` on the endpoint annotation. `aspire publish` fails fast with an `InvalidOperationException` from `EndpointRoutingValidation` if a non-external endpoint is routed, so always pair `AddIngress`/`AddGateway` plumbing with explicit external opt-in on the target resource. This applies to AKS through `AzureKubernetesEnvironment` as well.
8. Use `PublishAsKubernetesService(...)` / `publishAsKubernetesService(...)` only for per-resource Kubernetes manifest customization.

## Azure Kubernetes Service (AKS) setup

Expected package and AppHost environment:

```bash
aspire add azure-kubernetes
```

Add an Azure Kubernetes Service (AKS) environment resource using the C# or TypeScript API shape returned by Aspire docs.

Azure Kubernetes Service (AKS) deployment provisions Azure infrastructure, including Azure Kubernetes Service (AKS), ACR, managed identity, and Azure resources modeled in the AppHost.

Azure Kubernetes Service (AKS) creates an inner Kubernetes environment for Helm deployment and auto-creates an Azure Container Registry unless a registry is explicitly configured.

### Azure Kubernetes Service (AKS) code changes

Make these changes in the AppHost:

1. Run `aspire add azure-kubernetes` if the AppHost does not already reference the Azure Kubernetes hosting integration.
2. Add an Azure Kubernetes Service (AKS) environment resource.

   C# AppHost shape:

   ```csharp
   var aks = builder.AddAzureKubernetesEnvironment("aks");
   ```

   TypeScript AppHost shape:

   ```typescript
   const aks = await builder.addAzureKubernetesEnvironment("aks");
   ```

3. Do not add explicit compute-environment assignment for the common single-environment case. Only if the AppHost has multiple compute environments, disambiguate each Azure Kubernetes Service (AKS) workload; in C#, add `.WithComputeEnvironment(aks)` to each compute resource that should deploy there.
4. Do not add a plain `AddKubernetesEnvironment` next to `AddAzureKubernetesEnvironment` for the same target. The Azure Kubernetes Service (AKS) environment owns the inner Kubernetes/Helm environment.
5. Let the integration create the default Azure Container Registry unless the user needs a specific registry. If they do, use the Azure Kubernetes registry customization API from current docs.
6. Use node pool/subnet/customization APIs only when required, for example `WithSystemNodePool(...)`, `AddNodePool(...)`, `WithSubnet(...)`, or the TypeScript equivalents returned by API docs.
7. Use Kubernetes Gateway/Ingress APIs for public exposure; do not assume every service becomes public.

## Preflight

For all Kubernetes targets:

- `kubectl` is installed.
- `helm` is installed.
- AppHost has the correct Kubernetes environment.
- AppHost parameters are configured or can be prompted.
- External exposure is explicit through Ingress, Gateway API, service customization, or target-specific defaults.
- Helm is the default deployment engine; record any customized namespace, release name, chart name, or chart version.
- Storage defaults are understood. Kubernetes defaults to `emptyDir`; persistent storage needs explicit storage type/class/size decisions.

For existing/external clusters:

- `kubectl config current-context` points to the intended cluster.
- Container registry is configured and reachable.
- Registry authentication is configured for image push from the agent machine.
- Image pull secret or registry auth is configured when the cluster cannot pull from the registry anonymously.
- Provider-specific registry attachment is configured when required, for example attaching a provider registry to a managed cluster.
- Namespace/release/chart settings are understood if Helm settings are customized.

For Azure Kubernetes Service (AKS):

- Azure CLI auth is available (`az login` for local deploy).
- Subscription, location, and resource group source are known.
- `Azure:SubscriptionId` / `Azure__SubscriptionId` and `Azure:Location` / `Azure__Location` are configured or can be prompted.
- Node pool defaults fit the target subscription/region, or the AppHost customizes node pools.

## Preview and publish

Generate Helm artifacts without applying them:

```bash
aspire publish -o k8s-artifacts
```

For Azure Kubernetes Service (AKS), publish can also generate Azure infrastructure artifacts.

Inspect expected output:

- `Chart.yaml`
- `values.yaml`
- `templates/`
- generated environment values for deploy-time parameter and image resolution
- Bicep infrastructure for Azure Kubernetes Service (AKS) targets when generated

Use list steps before deploy:

```bash
aspire deploy --list-steps
```

## Deploy

Existing/external cluster:

```bash
aspire deploy
```

Aspire uses current `kubectl` context and Helm. It deploys application resources into the cluster; it does not create or delete an externally-managed Kubernetes cluster or provider registry.

Azure Kubernetes Service (AKS):

```bash
aspire deploy
```

Aspire provisions Azure resources, builds/pushes images to ACR, generates Helm charts, and installs them into Azure Kubernetes Service (AKS).

External Helm charts added to a Kubernetes environment install after the main app chart. They are not uninstalled by default during destroy because they may be shared; only treat them as owned by the Aspire app when the AppHost explicitly opts into destroy-time uninstall.

## Destroy

Run the destroy pipeline for this AppHost/environment:

```bash
aspire destroy --environment <name>
```

For an existing/external cluster, the destroy step runs against the configured cluster context for the selected AppHost/environment. Confirm the current `kubectl` context, namespace/release settings, and AppHost environment before running destroy. It does not delete an externally-created Kubernetes cluster, node pool, provider load balancer account resource, or container registry. Delete provider infrastructure with the provider CLI only when the user explicitly asks to remove that infrastructure too. For Azure Kubernetes Service (AKS), also confirm the Azure subscription and resource group because that target can own Azure infrastructure as well as cluster resources. Use `--yes` only after destructive intent is explicit. Use kubectl, cloud CLI, provider delete commands, or Helm only to diagnose failed teardown or remove leftovers that are not managed by the Aspire deployment target.

## Native artifact handoff

If the user wants to apply published artifacts themselves:

```bash
helm install <release> ./k8s-artifacts
helm upgrade <release> ./k8s-artifacts
```

Use values files or `--set` for environment-specific image tags and settings.

## Exposure and TLS

By default, generated services use the Kubernetes environment's default service type, which is `ClusterIP`. Public access requires an explicit exposure mechanism such as Ingress, Gateway API, or service type customization. For cloud Kubernetes providers without an ingress controller configured, a frontend `LoadBalancer` Service is often the fastest public smoke-test path, while backend services should usually remain `ClusterIP`. Verify exact APIs in docs before editing because C# and TypeScript AppHost shapes differ.

Gateway/Ingress TLS can add extra deployment steps for bootstrap secrets, FQDN discovery, and field ownership cleanup. If a deployment fails around TLS or Gateway resources, inspect the listed pipeline steps and the generated manifests before changing the AppHost.

## Troubleshooting

- Empty connection strings: inspect generated ConfigMaps and Secrets and verify env var names in pod specs.
- Password/auth failures: inspect Kubernetes Secrets and references; do not print secret values.
- ImagePullBackOff: verify image registry, pull secret/identity, and image tags.
- Azure Kubernetes Service (AKS) cluster unreachable: refresh credentials with `az aks get-credentials`.
- ACR pull issues on Azure Kubernetes Service (AKS): check ACR role assignment with `az aks check-acr`.
- Helm conflict on Gateway/TLS resources: inspect `helm status`, `kubectl describe gateway`, and the generated Gateway/Ingress manifests.
