---
name: aspire-deployment
description: "**WORKFLOW SKILL** — Deploy Aspire apps from AppHost models to Docker Compose, Kubernetes, Azure, or AWS. WHEN: \"deploy Aspire app\", \"publish Aspire artifacts\", \"deploy to Azure Container Apps\", \"generate Kubernetes artifacts\", \"tear down Aspire deployment\". INVOKES: aspire CLI, Aspire docs, target cloud/container CLIs. FOR SINGLE OPERATIONS: use generic Azure, Kubernetes, Docker, or AWS tools only when no Aspire AppHost exists."
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

# Aspire Deployment

Use this skill when the task is to publish, preview, validate, deploy, or tear down an Aspire application deployment. This skill owns Aspire deployment routing. Do not start with a generic Azure, Docker, Kubernetes, Helm, or Bicep workflow until you have checked whether the workspace is an Aspire app.

Aspire deployment starts from the AppHost model. Treat `aspire deploy`, `aspire publish`, `aspire destroy`, `aspire do`, and the deployment environment resources in the AppHost as the primary path.

Keep this as one skill with target-specific references. Load only the reference files that match the target you discover or the user requests.

## Routing precedence

This skill wins over generic cloud deployment skills when both conditions are true:

1. The user asks to deploy, publish, generate deployment artifacts, create Bicep/Helm/Compose/CDK output, host on Azure or AWS, deploy to Azure or AWS, deploy to Kubernetes, deploy to Docker Compose, tear down deployed resources, or validate a deployment.
2. The workspace has Aspire markers:
   - Aspire workspace configuration
   - C# or TypeScript AppHost files
   - an AppHost project
   - AppHost code using Aspire distributed application builder APIs

If Aspire markers are present but this skill was not automatically invoked, switch to this skill before continuing. Prefer Aspire CLI commands such as `aspire ls`, `aspire config list`, `aspire ps`, and `aspire describe` for workspace orientation.

## Guiding principles

### Use Aspire docs before changing deployment code

Before adding target packages, editing the AppHost, or using an unfamiliar deployment API, use Aspire docs:

```bash
aspire docs search "deploy with Aspire"
aspire docs search "Docker Compose deployment"
aspire docs search "Kubernetes deployment"
aspire docs search "Azure Container Apps deployment"
aspire docs search "Azure App Service deployment"
aspire docs search "Azure Kubernetes Service deployment"
aspire docs get "deploy-to-azure-kubernetes-service-aks"
aspire docs get "<slug-from-search-results>"
```

When you need exact C# or TypeScript API shape, use API docs too. Search both languages when you are not sure which AppHost language the repo uses:

```bash
aspire docs api search "<deployment API or concept>" --language csharp
aspire docs api search "<deployment API or concept>" --language typescript
aspire docs api get "<id-from-api-search>"
```

Do not invent package names, builder methods, overloads, or deployment commands. API shapes differ between C# and TypeScript AppHosts.

### Prefer Aspire-native deployment

Use Aspire deployment targets and CLI commands first:

```bash
aspire publish --list-steps
aspire deploy --list-steps
aspire publish
aspire deploy
aspire destroy
aspire do <step>
```

Use target-specific tooling only after Aspire has generated artifacts or when the target docs call for it:

- Docker Compose: inspect generated `aspire-output/docker-compose.yaml` and `.env*`; Aspire can also run `docker compose up` through `aspire deploy`.
- Kubernetes: inspect generated Helm chart output; use Helm/kubectl when applying published artifacts yourself.
- Azure: use `aspire add <azure-target>`, `aspire publish`, and `aspire deploy` through the AppHost deployment environment.
- AWS: use `aspire add aws` to add the integration, inspect generated CDK/CloudFormation output, and follow the AWS integrations repository guidance.

### Ask where to deploy only when ambiguous

Do not ask for target selection when the user already chose a target such as Docker Compose, Kubernetes, Azure Container Apps, Azure App Service, Azure Kubernetes Service (AKS), or AWS. Use the chosen target and continue with its reference.

If the user did not explicitly choose a deployment target and the AppHost does not already contain exactly one deployment environment, ask where they want to deploy before adding integrations, editing the AppHost, publishing artifacts, or deploying. Use a single multiple-choice question:

> Where do you want to deploy this Aspire app?

Show these choices:

| Choice | Aspire add command | Use when |
|--------|--------------------|----------|
| Docker Compose | `aspire add docker` | The user wants local/server container deployment artifacts for Docker or Podman. |
| Kubernetes | `aspire add kubernetes` | The user has an existing Kubernetes cluster and wants Helm/Kubernetes artifacts or direct cluster deployment. |
| Azure Container Apps | `aspire add azure-appcontainers` | The user wants an Azure-managed container platform for distributed apps and services. |
| Azure App Service | `aspire add azure-appservice` | The user wants Azure website hosting for web apps/APIs that fit the App Service model. |
| Azure Kubernetes Service (AKS) | `aspire add azure-kubernetes` | The user wants Aspire to provision and deploy to Azure-managed Kubernetes. |
| AWS | `aspire add aws` | The user wants Aspire to publish/deploy through the AWS Aspire integrations and AWS CDK. |

If the user says only "Azure", ask again with just the Azure choices: Azure Container Apps, Azure App Service, or Azure Kubernetes Service (AKS). If the AppHost already contains exactly one deployment environment and the user did not ask to change targets, use that target and tell the user what was detected.

### Ask before creating cloud resources when intent is not explicit

Cloud deploys can create billable resources. If the user asked for a plan, preview, validation, or "make this deployable", stop after the deployment plan/artifacts and ask before running the command that provisions resources.

If the user explicitly asked to deploy now, continue through preflight and deployment, but still surface any target choice, subscription/resource group ambiguity, or missing parameter decisions before provisioning.

### Keep Azure deployment Aspire-native

The Azure deployment path in this skill is `aspire add <azure-target>`, AppHost environment configuration, `aspire publish`, and `aspire deploy`. Do not route Azure deployment work through a separate Azure deployment tool or generated infrastructure workflow.

## Default workflow

1. **Orient to the Aspire workspace.**
   - Start with `aspire ls` to list AppHosts in the current scope, then use `aspire.config.json`, AppHost project files, or `aspire ps` if more context is needed.
   - If no AppHost exists, stop deployment work and invoke the `aspireify` skill to initialize/wire the AppHost before continuing.
   - Identify C# vs TypeScript AppHost.
   - Prefer Aspire CLI commands for discovery and state inspection.
2. **Clarify or infer the deployment target.**
   - If the user named Docker Compose, Kubernetes, Azure Container Apps, Azure App Service, Azure Kubernetes Service (AKS), or AWS, load that target reference without asking again.
   - If they only said "deploy", inspect existing AppHost target environment resources.
   - If exactly one target environment already exists, use it and state what was detected.
   - If multiple targets exist, none exists, or the user says only "Azure", ask where to deploy using the choices above.
3. **Load target and app-type references.**
    - Docker Compose: [references/docker-compose.md](references/docker-compose.md)
    - Kubernetes and Azure Kubernetes Service (AKS): [references/kubernetes.md](references/kubernetes.md)
    - Azure Container Apps/App Service/Azure Kubernetes Service (AKS): [references/azure.md](references/azure.md)
    - AWS: [references/aws.md](references/aws.md)
    - JavaScript app resources: [references/javascript.md](references/javascript.md)
    - CI/CD or GitHub Actions automation: [references/cicd.md](references/cicd.md)
4. **Use Aspire docs search for current guidance.**
    - Search and get the target deployment docs.
    - Search API docs before editing AppHost code.
5. **Apply the target code changes.**
   - Run the target's `aspire add ...` command if the integration is missing.
   - Add the deployment environment resource to the AppHost.
   - Do not add explicit compute-environment assignment for the common single-environment case. Only disambiguate when the AppHost has multiple deployment environments. In C# this is usually `WithComputeEnvironment(...)`; for TypeScript AppHosts, verify the current language-specific docs before assuming an equivalent.
   - Add only the target-specific customization APIs the deployment needs, such as endpoint exposure, Helm settings, Compose file customization, Azure site/container app customization, or AWS publish target overrides.
6. **Preflight the deployment model.**
   - Confirm the target integration package exists in the AppHost.
   - Confirm the AppHost has the target environment resource.
   - Confirm compute resources are assigned to the target environment only when multiple compute environments exist. A single compute environment is the common case and can be inferred.
   - Inventory parameters, secrets, connection strings, external endpoints, container registries, and target-specific prerequisites.
   - For Azure or AWS, confirm auth, target account/subscription, region/location, and resource group/stack context.
7. **Preview before applying.**
   - Run `aspire publish --list-steps` or `aspire deploy --list-steps`.
   - Use `aspire publish -o <scratch-or-output-path>` when artifact review is requested.
   - Treat published artifacts as a preview/handoff. `aspire deploy` resolves values and applies the deployment from the AppHost model; it does not consume a previously published output directory.
   - Summarize resources, endpoints, parameters, secrets, identities, and generated artifacts.
8. **Deploy or hand off.**
   - Run `aspire deploy` when the user asked to deploy and preflight is complete.
   - Run a named step with `aspire do <step>` only when the user asked for a specific pipeline step.
   - For published artifacts, explain the target-native apply step.
9. **Destroy only when explicitly requested.**
   - Run `aspire destroy` to execute the selected AppHost/environment's target destroy pipeline.
   - Confirm the AppHost, environment, target account/subscription/cluster, and destructive intent before running it.
   - Use `--yes` only when the user or CI workflow already made teardown intent explicit.
   - Prefer `aspire destroy` over target-native delete commands unless you are troubleshooting failed teardown or cleaning up unmanaged leftovers.
10. **Verify the outcome.**
   - Use target output, `aspire describe`, cloud CLI, Docker Compose, kubectl, or endpoint checks appropriate to the target.
   - After destroy, verify target resources are removed or record any leftovers that require manual cleanup.

## AppHost target detection

Search the AppHost for deployment environment resources:

| Target | Aspire add command | Integration | AppHost environment concept |
|--------|--------------------|-------------|-----------------------------|
| Docker Compose | `aspire add docker` | Docker hosting | Docker Compose environment |
| Kubernetes | `aspire add kubernetes` | Kubernetes hosting | Kubernetes environment |
| Azure Container Apps | `aspire add azure-appcontainers` | Azure Container Apps hosting | Azure Container Apps environment |
| Azure App Service | `aspire add azure-appservice` | Azure App Service hosting | Azure App Service environment |
| Azure Kubernetes Service (AKS) | `aspire add azure-kubernetes` | Azure Kubernetes hosting | Azure Kubernetes Service (AKS) environment |
| AWS | `aspire add aws` | AWS hosting | AWS CDK environment |

Use this table only for orientation. Before editing code, verify the current API in Aspire docs for the AppHost language.

## Parameter and secret preflight

Parameters are deployment inputs. They may be supplied by configuration files, user secrets, environment variables, command-line args, interactive prompts, or CI/CD secret stores depending on the target and command.

Before deployment, report:

- Parameter name from AppHost parameter APIs, including config-backed parameters
- Whether it is secret
- Expected provider syntax such as `Parameters__name`; for parameter names with dashes, use underscores in environment variables, for example `registry-endpoint` becomes `Parameters__registry_endpoint`
- Where it flows, such as a project environment variable, connection string, Key Vault secret, Helm Secret, Compose `.env`, or Azure app setting
- Whether a value appears configured or missing

Use `aspire secret list` for AppHost user secrets when appropriate, but do not print secret values. For deployment artifacts, inspect generated placeholders and mappings, not raw secret content.

## Target references

- [references/docker-compose.md](references/docker-compose.md) - Docker Compose target, generated files, environment variables, cleanup.
- [references/kubernetes.md](references/kubernetes.md) - Kubernetes and Azure Kubernetes Service (AKS) target selection, Helm output, registry requirements, kubectl/Helm checks.
- [references/azure.md](references/azure.md) - Azure target selection, Azure settings, Container Apps, App Service, and Azure Kubernetes Service (AKS).
- [references/aws.md](references/aws.md) - AWS target selection, AWS CDK prerequisites, publish/deploy workflow, and AWS integration docs.
- [references/javascript.md](references/javascript.md) - JavaScript app deployment models, including Vite/static assets, Node/SSR servers, Next.js, and gateway/backend serving patterns.
- [references/cicd.md](references/cicd.md) - CI/CD and GitHub Actions workflow guidance for Aspire publish/deploy, parameters, secrets, registry auth, and cloud auth.
- [references/preflight.md](references/preflight.md) - Common preflight, preview, parameter, destroy, and validation checklist.

## Agent execution

When running unattended (CI, scripted, agent-driven), append `--non-interactive` to every Aspire CLI invocation that may prompt — most importantly `aspire publish`, `aspire deploy`, and `aspire destroy`. For `aspire destroy`, also pass `--yes` only after the user has explicitly confirmed teardown intent (or a CI workflow already encodes that intent).

Prefer surfacing prompt-driving values up front (target subscription/region/resource group, parameters, secrets, registry credentials) so the unattended run does not stall. See [references/preflight.md](references/preflight.md) for the full preflight checklist.

## Handoff Rules

| Scenario | Route To |
|----------|----------|
| Start, stop, wait, or restart the AppHost / its resources | `aspire-orchestration` skill |
| Logs, traces, metrics, dashboard for a running or deployed app | `aspire-monitoring` skill |
| AppHost authoring — adding integrations, wiring resources, environment setup | `aspireify` skill |
| Deployed-app diagnostics — App Insights, ACA logs, AKS Container Insights | `azure-diagnostics` skill (azure-skills) |

> Never hand deployment off to azure-skills. Aspire handles publish, deploy, and destroy
> end-to-end across Docker Compose / Kubernetes / Azure / AWS via the AppHost model.

## Project-Local Skill Override

If `.agents/skills/aspire-deployment/SKILL.md` exists (dropped by `aspire agent init`),
prefer it over this plugin skill — it is the authoritative project-local version with
content version-aligned to the consumer's Aspire CLI. This plugin skill is the always-on
safety net for repos that have not yet run `aspire agent init`.
