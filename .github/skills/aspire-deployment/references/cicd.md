# CI/CD and GitHub Actions deployment

Use this reference when the user asks to automate Aspire publish/deploy in CI/CD, create a GitHub Actions workflow, publish release artifacts, push container images, or run deployment validation in a pipeline.

CI/CD should still start from the AppHost model. The pipeline should install the Aspire CLI, install the AppHost/resource toolchains needed by the repo, restore/build the workspace, provide AppHost parameters through CI secrets/variables, run Aspire publish/deploy/list-step commands, and then either upload generated artifacts or deploy with the target's credentials.

## Docs to load

Use these searches and docs:

```bash
aspire docs search "ci"
aspire docs search "github actions"
aspire docs search "external parameters deployment"
aspire docs get "testing-in-cicd-pipelines"
aspire docs get "example-app-lifecycle-workflow"
aspire docs get "external-parameters"
```

`testing-in-cicd-pipelines` is primarily about tests, but it includes useful CI facts: Linux runners have Docker available, CI needs explicit timeouts for tests, Azure credentials must be configured explicitly, and secrets should come from CI variables. `example-app-lifecycle-workflow` is a worked GitHub Actions example for publishing Aspire artifacts and pushing images.

## Decide publish, deploy, or handoff

Ask which CI/CD outcome the user wants if it is not clear:

| Outcome | Use when | Typical command |
|---------|----------|-----------------|
| Validate deployment model | PR or pre-merge check should prove the AppHost can produce deployment steps/artifacts | `aspire publish --list-steps` or `aspire deploy --list-steps` |
| Publish artifacts | CI should produce Compose/Helm/CDK/Bicep or other target artifacts for review or later apply | `aspire publish -o <output>` |
| Push images only | CI should build/push project images but not deploy infrastructure | `aspire do <push-step>` after checking `aspire deploy --list-steps` |
| Deploy from CI | Protected branch/environment should provision/update target infrastructure | `aspire deploy --environment <name>` |
| Destroy from CI | Explicit cleanup workflow should tear down an Aspire-owned deployment | `aspire destroy --environment <name> --yes` |

Do not assume `aspire deploy` consumes a previous `aspire publish` output. `aspire deploy` applies directly from the AppHost model and resolves values for the selected environment.

## GitHub Actions workflow shape

Use this baseline shape, then add the setup block that matches the AppHost/resource graph and layer target-specific auth and parameters on top:

```yaml
name: Deploy

on:
  workflow_dispatch:
  push:
    branches: [main]

permissions:
  contents: read
  packages: write
  id-token: write

jobs:
  publish:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Install Aspire CLI
        run: |
          curl -sSL https://aspire.dev/install.sh | bash
          echo "$HOME/.aspire/bin" >> "$GITHUB_PATH"

      - name: Inspect Aspire deployment steps
        run: |
          aspire ls
          aspire publish --list-steps
          aspire deploy --list-steps
```

Add one or both setup blocks before Aspire commands:

| AppHost/resources | Setup guidance |
|-------------------|----------------|
| C# AppHost or .NET project resources | Install .NET with `actions/setup-dotnet`, then use the repo's restore/build command. |
| TypeScript AppHost or JavaScript resources | Install Node with `actions/setup-node`, then run the repo's package-manager install/build commands. |
| Mixed C#/TypeScript graph | Use both setup blocks so the AppHost and all compute resources can be restored, built, and published. |

C# AppHost setup:

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: 10.0.x

- name: Restore and build .NET workspace
  run: |
    dotnet restore
    dotnet build --no-restore
```

TypeScript AppHost setup:

```yaml
- name: Setup Node
  uses: actions/setup-node@v4
  with:
    node-version: 22.x

- name: Install and build TypeScript workspace
  run: |
    npm ci
    npm run build --if-present
```

Use repository-specific setup instead of the generic snippets when the repo already has wrapper scripts, a local SDK restore step, a package manager other than npm, an AppHost `package.json` in a subdirectory, or a required build command.

## Parameters and secrets

Map AppHost parameters through workflow `env:` using Aspire configuration environment-variable conventions:

```yaml
env:
  Parameters__registry_endpoint: ghcr.io
  Parameters__registry_repository: ${{ github.repository }}
  Parameters__api_key: ${{ secrets.API_KEY }}
```

Rules:

- AppHost parameter `name` maps to `Parameters__name`.
- Parameter names with dashes use underscores in environment variables, for example `registry-endpoint` becomes `Parameters__registry_endpoint`.
- Secret parameters should come from GitHub Actions secrets or environment secrets.
- Do not print secret values. Prefer listing parameter names/status before deployment.
- Use GitHub Environments for production secrets and required reviewers when `aspire deploy` provisions cloud resources.

## Container registry auth

For GitHub Container Registry, authenticate before Aspire push/publish/deploy steps that build and push images:

```yaml
- name: Login to GHCR
  uses: docker/login-action@v3
  with:
    registry: ghcr.io
    username: ${{ github.actor }}
    password: ${{ secrets.GITHUB_TOKEN }}
```

Then provide registry parameters if the AppHost models them:

```yaml
- name: Push images with Aspire
  env:
    Parameters__registry_endpoint: ghcr.io
    Parameters__registry_repository: ${{ github.repository }}
  run: aspire do <push-step>
```

Get the exact push/build step from `aspire deploy --list-steps` or `aspire publish --list-steps`; do not hardcode `push` unless the target docs/listed steps show it.

## Publish artifacts in GitHub Actions

Use publish when the pipeline should produce artifacts for review or later apply:

```yaml
- name: Publish Aspire artifacts
  env:
    Parameters__registry_endpoint: ghcr.io
    Parameters__registry_repository: ${{ github.repository }}
  run: aspire publish -o ./aspire-output

- name: Upload Aspire artifacts
  uses: actions/upload-artifact@v4
  with:
    name: aspire-output
    path: ./aspire-output
```

Treat published output as potentially sensitive. Docker Compose `.env.<environment>`, Helm values/secrets, CDK output, and target-specific deployment state can include resolved parameter values or secret references. Upload only what the user intends to retain.

## Deploy from GitHub Actions

Use deploy only when the workflow is intentionally allowed to modify infrastructure:

```yaml
- name: Deploy with Aspire
  env:
    Parameters__registry_endpoint: ghcr.io
    Parameters__registry_repository: ${{ github.repository }}
  run: aspire deploy --environment production --non-interactive
```

Add target-specific authentication before this step:

- Docker Compose: runner needs Docker/Podman and access to the target Docker host if deploying remotely.
- Kubernetes: configure `kubectl` context and registry pull auth before `aspire deploy`.
- Azure: authenticate Azure CLI or Azure SDK credentials before `aspire deploy`; set `Azure__SubscriptionId`, `Azure__Location`, and optionally `Azure__ResourceGroup`.
- AWS: configure AWS credentials/region, install CDK prerequisites, and bootstrap the account/region before `aspire deploy`.

Never route Azure deployment through a separate Azure deployment tool from this skill. Keep Azure deployment Aspire-native with `aspire deploy`, and use Azure CLI only for authentication and live state inspection.

## Destroy from CI/CD

Use `aspire destroy` for teardown workflows that intentionally run the AppHost deployment target's destroy pipeline:

```yaml
- name: Destroy with Aspire
  if: ${{ github.event_name == 'workflow_dispatch' }}
  env:
    Azure__SubscriptionId: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    Azure__Location: ${{ vars.AZURE_LOCATION }}
    Azure__ResourceGroup: ${{ vars.AZURE_RESOURCE_GROUP }}
  run: aspire destroy --environment production --yes --non-interactive
```

Keep destroy jobs manually triggered or gated by a protected GitHub Environment. Reuse the same target authentication, AppHost path, environment, and parameter conventions as deploy. Do not put destroy into normal validation or deploy jobs unless the workflow owns temporary infrastructure and teardown is part of the tested lifecycle. If CI also created external infrastructure that is outside the Aspire deployment target, such as a temporary Kubernetes cluster or registry, clean that up in separate explicit provider-specific steps after the Aspire destroy step.

## Azure GitHub Actions auth

Use the repository's preferred Azure auth pattern. For OIDC with `azure/login`, ensure the workflow has `id-token: write` and the cloud app/federated credential is configured:

```yaml
- name: Azure login
  uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

Then provide Aspire Azure settings:

```yaml
env:
  Azure__SubscriptionId: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
  Azure__Location: ${{ vars.AZURE_LOCATION }}
  Azure__ResourceGroup: ${{ vars.AZURE_RESOURCE_GROUP }}
```

If the workflow uses service-principal secrets instead of OIDC, set `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_CLIENT_SECRET` as secrets for Azure SDK authentication. Do not echo these values.

## Azure GitHub Actions deployment pattern

A real Azure Aspire deployment workflow can be as small as checkout, AppHost toolchain setup, Aspire CLI install, Azure login, and `aspire deploy`. Use these checked-in workflow references when the user wants GitHub Actions to deploy directly to Azure through Aspire and gate it with a GitHub Environment:

- C# AppHost: [github-actions-azure-csharp.yml](github-actions-azure-csharp.yml)
- TypeScript AppHost: [github-actions-azure-typescript.yml](github-actions-azure-typescript.yml)

The C# AppHost reference has this shape:

```yaml
name: Aspire Deploy CI/CD

on:
  push:
    branches: [live]

permissions:
  id-token: write
  contents: read

jobs:
  aspire_deploy:
    runs-on: ubuntu-latest
    name: Deploy with Aspire
    environment:
      name: production
    env:
      DOTNET_CLI_TELEMETRY_OPTOUT: 1
      DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 1
      DOTNET_NOLOGO: true
    steps:
      - uses: actions/checkout@v4
        with:
          submodules: true
          lfs: false

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
          dotnet-quality: preview

      - name: Install Aspire CLI
        run: curl -sSL https://aspire.dev/install.sh | bash

      - name: Add Aspire CLI to PATH
        run: echo "$HOME/.aspire/bin" >> "$GITHUB_PATH"

      - name: Azure Login
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy with Aspire
        run: aspire deploy --apphost ./src/apphost.cs --environment Production --non-interactive
        env:
          Azure__SubscriptionId: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
          Azure__Location: ${{ vars.AZURE_LOCATION || 'eastus' }}
          Azure__ResourceGroup: ${{ vars.AZURE_RESOURCE_GROUP }}
          Parameters__admin_password: ${{ secrets.ADMIN_PASSWORD }}
```

Create a GitHub Environment named `production` and store deployment values there so approvals, branch rules, environment variables, and environment secrets apply to the deployment job:

| GitHub Environment value | Example names |
|--------------------------|---------------|
| Variables | `AZURE_LOCATION`, `AZURE_RESOURCE_GROUP`, app build variables that are safe to expose |
| Secrets | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, AppHost parameter secrets such as `ADMIN_PASSWORD` |

Adapt the example instead of copying it blindly:

- Start from the external `.yml` reference that matches the AppHost language, then adjust paths, package manager commands, package-manager caching, target branch, and parameter names.
- Use `--apphost <path>` when the workflow should pin a specific AppHost, such as a single-file `apphost.cs`, `apphost.ts`, or an AppHost project file.
- For TypeScript AppHosts, replace the .NET setup with Node/package-manager setup, deploy with `--apphost <path-to-apphost.ts> --non-interactive`, and provide deployment settings and AppHost parameters through the deploy step's `env:`.
- Keep `id-token: write` for Azure OIDC login.
- Put non-secret deployment settings in GitHub Environment variables when possible, such as `AZURE_LOCATION` and `AZURE_RESOURCE_GROUP`.
- Put secret AppHost parameters in GitHub Environment secrets and pass them as `Parameters__*`.
- Add app-specific build metadata only when the app consumes it. For example, Vite apps need a `VITE_` prefix for values intended for client-side build-time exposure.
- Add extra Azure settings only when the AppHost declares them, such as `Azure__PostgresLocation` for a custom PostgreSQL region.
- Add a separate preflight step with `aspire ls` and `aspire deploy --list-steps` when the workflow should show planned deployment steps before applying changes.

## Validation and troubleshooting

Before applying changes:

```bash
aspire ls
aspire publish --list-steps
aspire deploy --list-steps
```

After deployment, use the target reference's validation commands:

- Docker Compose: `docker compose ps` against generated files or the target Compose project.
- Kubernetes: `kubectl get pods`, `kubectl get svc`, and `helm status`.
- Azure: `az account show`, `az resource list`, target-specific `az containerapp`, `az webapp`, or `az aks` commands.
- AWS: `aws cloudformation describe-stacks`, stack events, and service-specific AWS CLI commands.

Common CI/CD failures:

- Missing parameter: add `Parameters__*` env vars or GitHub Environment secrets.
- Wrong registry path: compare `Parameters__registry_endpoint` and `Parameters__registry_repository` to generated image names.
- Docker unavailable: use `ubuntu-*` GitHub-hosted runners for Linux containers or a configured self-hosted runner.
- Cloud auth uses the wrong identity/subscription/account: print identity metadata only, not secrets.
- Publish output contains sensitive values: reduce uploaded paths or use environment-protected artifacts.
- Deployment step name changed: rerun `aspire deploy --list-steps` and update `aspire do <step>`.
