# Common deployment preflight

Use this reference for every Aspire deployment target.

## AppHost discovery

Find the AppHost before choosing commands:

1. Run `aspire ls` first. It lists all AppHosts in the current scope and is the preferred discovery command.
2. If `aspire ls` shows exactly one AppHost, use it.
3. If `aspire ls` shows no AppHosts, stop deployment work and invoke the `aspireify` skill to initialize/wire the AppHost before continuing.
4. If `aspire ls` shows multiple AppHosts or discovery is still ambiguous, inspect `aspire.config.json`, `*.AppHost.csproj`, `apphost.cs`, or `apphost.ts`.
5. For C# project AppHosts, confirm the project references Aspire AppHost support.
6. For C# single-file AppHosts, look for the Aspire AppHost SDK directive.
7. For TypeScript AppHosts, look for the AppHost file and generated module support.

Use `--apphost <path>` when discovery is ambiguous, multiple AppHosts exist, or CI/CD should pin a specific AppHost. The path can point to an AppHost project file or supported single-file AppHost, such as `apphost.cs`.

## Docs lookup checklist

Use Aspire docs search before changing target configuration:

```bash
aspire docs search "ci"
aspire docs search "github actions"
aspire docs search "deployment overview"
aspire docs search "deploy with Aspire"
aspire docs search "external parameters deployment"
aspire docs search "<target> deployment"
aspire docs get "<slug>"
```

Use API docs before writing AppHost code. Search both languages until the AppHost language is known:

```bash
aspire docs api search "<method-or-resource>" --language csharp
aspire docs api search "<method-or-resource>" --language typescript
aspire docs api get "<id>"
```

If `aspire docs` is unavailable, use official `aspire.dev` docs through the available web/documentation tools. Do not use outdated blog posts or workload-era docs as authority.

## Target detection checklist

Inspect AppHost code for existing target environments. API names differ by AppHost language, so use these as concepts rather than exact names:

- Docker Compose environment
- Kubernetes environment
- Azure Container Apps environment
- Azure App Service environment
- Azure Kubernetes Service (AKS) environment
- AWS CDK environment

If none exists, ask for the deployment target unless the user's request clearly names one.

If more than one exists, ask which one to use or whether to deploy all.

## Compute environment assignment

Aspire can infer the compute environment only when there is exactly one compute environment in the model. If multiple deployment environments exist, verify each compute resource is explicitly assigned to the intended environment before publishing or deploying.

Do not assume a resource deploys just because it appears in run mode. Resources hidden behind run-mode-only conditionals or assigned to a different compute environment will not be part of the target deployment.

## Preview commands

Use these before applying changes:

```bash
aspire publish --list-steps
aspire deploy --list-steps
```

Treat `--list-steps` as a structural preview, not proof that a later deploy can run unattended. Some targets can emit deploy-time selection prompts that are not AppHost parameters, such as Azure tenant selection. If a prompt appears, answer it in a real interactive terminal/PTY; do not try to satisfy it by setting unrelated AppHost secrets unless the target docs explicitly define that mapping.

Use publish when the user wants to inspect generated artifacts:

```bash
aspire publish -o ./aspire-output
```

The output path can be a scratch path if the user only asked for a preview. Do not commit generated deployment artifacts unless the user explicitly asks to keep them in source control.

When running a command that may prompt, do not pipe it through `tee`, `tail`, or similar output filters. Pipes can remove the interactive terminal that selection prompts require. Use the shell/session transcript or the CLI log file path printed by Aspire for diagnostics.

`aspire publish` and `aspire deploy` are related but not a two-step apply pipeline:

- `aspire publish` generates target artifacts for review or handoff and leaves unresolved values as target-specific placeholders where possible.
- `aspire deploy` resolves parameters and applies deployment steps directly from the AppHost model.
- Running `aspire deploy` later does not consume the directory produced by a previous `aspire publish`.

Use `--environment <name>` when the user wants a staging/production context other than the default. Deployment state and cached values are scoped by AppHost and environment, so changing the environment changes which cached values are used.

## Destroying deployments

Use `aspire destroy` to run the AppHost deployment environment's destroy pipeline:

```bash
aspire destroy --environment <name>
```

Treat destroy as a destructive deployment operation:

- Run it only when the user explicitly asks to tear down or clean up a deployment, or when a test workflow owns temporary infrastructure and teardown is part of that workflow.
- Use `aspire destroy --list-steps` when practical to preview the teardown pipeline before applying it.
- Confirm the AppHost, environment, target account/subscription/cluster, and resource group/namespace/stack context before applying.
- Use the same `--apphost <path>` and `--environment <name>` values used for deployment when discovery or environment scope could be ambiguous.
- Use `--yes` only for non-interactive teardown when destructive intent has already been approved, such as an explicit cleanup job.
- Do not describe destroy as a Helm, Kubernetes, Docker, Azure, or AWS command. It is an Aspire command that delegates to the selected deployment target's destroy step.
- Keep target-native delete commands as troubleshooting or manual-leftover cleanup, not the primary teardown path for resources managed by the Aspire deployment target.

## Parameters and secrets

Inventory parameter declarations. API casing differs by AppHost language:

- direct parameters
- secret parameters
- config-backed parameters
- connection string parameters
- target-specific parameter APIs

Report each parameter without revealing values:

| Field | What to report |
|-------|----------------|
| Name | AppHost parameter name |
| Secret | whether it is marked secret |
| Source | environment variable, appsettings, user secrets, command line, prompt, CI secret |
| Consumer | project env var, connection string, Key Vault secret, Compose env, Helm Secret, Azure app setting |
| Status | configured, missing, generated, or unknown |

Use these conventions:

- AppHost parameter config key: `Parameters:name`
- Environment variable provider key: `Parameters__name`
- Parameter names with dashes use underscores in environment variables, for example `registry-endpoint` becomes `Parameters__registry_endpoint`
- Local/dev-only AppHost secret command: `aspire secret set "Parameters:name" "<value>"`

`aspire secret set` is a local/dev convenience today, not the deployment parameter path. For TypeScript AppHosts and non-interactive runs, set deploy-time values as environment variables on the `aspire deploy` process:

```bash
Parameters__name="<value>" \
aspire deploy --apphost ./apphost.ts --environment Production --non-interactive
```

Never print secret values. If a command prints secrets, redact them in any summary.

Deployment state can include resolved parameter values. Treat local deployment cache files and generated environment-specific artifacts as sensitive unless the target docs prove otherwise.

For CI/CD and GitHub Actions, load `references/cicd.md` before adding workflow YAML or pipeline commands.

## External endpoints and production topology

Flag production-only endpoint behavior:

- External HTTP endpoint configuration can change what becomes public in deployment.
- JavaScript app resources need an explicit production serving model. Load `references/javascript.md` when the AppHost contains JavaScript, Vite, Node, or Next.js resources.
- Azure Container Apps supports internal and external endpoints, with one main HTTP ingress group.
- Azure App Service supports a public website model with external HTTP/HTTPS endpoints only.
- Kubernetes exposes services through ClusterIP by default unless Ingress, Gateway API, or service type customization is configured.

Report what will be public, internal, or not exposed based on the target docs and AppHost code.

## Run vs publish/deploy branching

Check for AppHost conditionals around execution context and environment:

- run mode vs publish/deploy mode
- development, staging, production, or custom environment checks

Explain when resources exist only locally or only in publish/deploy mode. If a resource is behind a run-mode-only branch, do not tell the user it will deploy.

## Target-specific tools

Use target-native tools only after Aspire has produced artifacts or when verifying the result:

- Docker Compose: inspect generated Compose files or run Compose status commands against the generated project/files.
- Kubernetes: inspect Helm output and use kubectl/Helm against the target cluster.
- Azure: use Azure CLI for authentication and resource inspection, while keeping deployment through Aspire target environments.
- AWS: use AWS/CDK tooling required by the AWS Aspire integration.

## Validation

After deployment, verify with target-appropriate checks:

- Docker Compose: `docker compose ps` against generated files or endpoint checks.
- Kubernetes: `kubectl get pods`, `kubectl get svc`, `helm status`, endpoint checks.
- Azure: CLI output, Azure CLI resource inspection, endpoint checks, and dashboard URL when available.

Do not mark cloud deployment complete until provisioning, deployment, and at least one target-specific health or endpoint check succeeded.
