# Docker Compose deployment

Use this reference when the user asks for Docker Compose deployment, local container deployment artifacts, or a non-cloud deployment package.

## Docs to load

Always start with current docs:

```bash
aspire docs search "Docker Compose deployment"
aspire docs search "Docker hosting integration"
aspire docs search "Docker Compose environment variables"
aspire docs get "deploy-to-docker-compose"
aspire docs get "docker-integration"
aspire docs get "<slug>"
```

Use API docs before editing. Search in the AppHost language you detected:

```bash
aspire docs api search "Docker Compose environment" --language csharp
aspire docs api search "Docker Compose environment" --language typescript
aspire docs api search "Docker Compose service customization" --language csharp
aspire docs api search "Docker Compose service customization" --language typescript
```

## Target setup

Expected package and AppHost environment:

```bash
aspire add docker
```

Add a Docker Compose environment resource using the C# or TypeScript API shape returned by Aspire docs. When a Docker Compose environment exists, compatible resources are automatically included in generated Compose output. Use the per-resource Docker Compose customization API only for customization.

## Code changes to make

Make these changes in the AppHost, not in the generated Compose output:

1. Run `aspire add docker` if the AppHost does not already reference the Docker hosting integration.
2. Add a Docker Compose environment resource.

   C# AppHost shape:

   ```csharp
   var compose = builder.AddDockerComposeEnvironment("docker-compose");
   ```

   TypeScript AppHost shape:

   ```typescript
   const compose = await builder.addDockerComposeEnvironment("docker-compose");
   ```

3. Do not add explicit compute-environment assignment for the common single-environment case. Only if the AppHost has multiple compute environments, disambiguate each Docker Compose workload; in C#, add `.WithComputeEnvironment(compose)` to each compute resource that should land in Compose.
4. For TypeScript AppHosts, verify the current language-specific docs before assuming an equivalent assignment API.
5. Keep normal app model relationships such as `WithReference`, endpoints, parameters, and connection strings in the AppHost. They flow into Compose environment variables and service dependencies.
6. Use customization APIs only for real Compose customization:
   - C#: `compose.ConfigureComposeFile(...)`, `compose.ConfigureEnvFile(...)`, and `resource.PublishAsDockerComposeService(...)`.
   - TypeScript: `compose.configureComposeFile(...)`, `compose.configureEnvFile(...)`, and `resource.publishAsDockerComposeService(...)`.

Do not hand-edit `docker-compose.yaml` as the durable fix unless the user explicitly wants to eject generated artifacts.

## Preflight

Check:

- Docker or Podman is installed and running.
- Docker must be at least 28.0.0, and Podman must be at least 5.0.0 for current Aspire CLI environment checks.
- The AppHost has a Docker Compose environment resource.
- The repo does not rely on local bind mounts that will be invalid on the target Docker host.
- Parameters and secrets are represented as placeholders in `.env` after `aspire publish` and resolved in `.env.<environment>` after prepare/deploy.
- Any fixed ports are intentional and do not conflict on the deployment host.

Use `ASPIRE_CONTAINER_RUNTIME=docker` or `ASPIRE_CONTAINER_RUNTIME=podman` only when the user needs to force a runtime.

## Preview and publish

Generate artifacts without starting containers:

```bash
aspire publish
```

Expected output includes:

- `aspire-output/docker-compose.yaml`
- `aspire-output/.env`
- resource Dockerfiles when needed

For environment-specific output and image build without running the whole deploy, use the target's prepare step if docs/list-steps show it:

```bash
aspire deploy --list-steps
aspire do prepare-docker-compose --environment staging
```

The exact step name depends on the Docker Compose environment resource name. Use the step shown by `aspire deploy --list-steps`: for an environment resource named `docker-compose`, the prepare step is `prepare-docker-compose`; for one named `compose`, it is `prepare-compose`.

## Deploy and destroy

Deploy:

```bash
aspire deploy
```

Aspire generates Compose output, builds images, writes environment-specific `.env` files, and runs Compose.

Run the Docker Compose target's destroy pipeline only when requested:

```bash
aspire destroy
```

For Docker Compose, `aspire destroy` delegates to the Compose deployment target for the selected AppHost/environment. Use Docker or Compose commands after destroy only to verify cleanup or investigate leftover containers, networks, volumes, or generated files.

## Common decisions

### Publish-only vs deploy

Use `aspire publish` when the user wants files to review or hand to another deployment system. Use `aspire deploy` when they want Aspire to start the Compose deployment.

`aspire publish` writes `docker-compose.yaml` and `.env` with blank placeholders for captured values. Prepare/deploy writes `.env.<environment>` with resolved values. Do not expect `aspire deploy` to consume a previously published directory.

### Customizing generated Compose

Use docs-backed APIs:

- The Compose file customization API for global Compose model changes.
- The environment file customization API for generated `.env` changes.
- The per-resource Docker Compose service customization API for service-level changes.

Do not hand-edit generated `docker-compose.yaml` as the source of truth unless the user explicitly wants to eject the artifact.

### Environment files and bind mounts

Generated `.env` values are intentionally separated from environment-specific `.env.<environment>` values:

- `.env` is a publish-time placeholder file and preserves existing user values when possible.
- `.env.<environment>` is written by prepare/deploy with resolved parameter, image, and bind-mount values.
- Project images are represented through image placeholders such as `<RESOURCE>_IMAGE` until prepare/deploy resolves them.
- Bind mount source paths are replaced by `<RESOURCE>_BINDMOUNT_<index>` placeholders because local paths often do not exist on another Docker host.
- Docker socket mounts are left as the platform socket path instead of being placeholderized.

Treat `.env.<environment>` as potentially sensitive.

### Compose project name

Aspire uses a generated Compose project name based on the environment resource name and, when available, the AppHost path hash. This prevents common collisions between different AppHosts using the same environment name.

### Service host names

When a service needs another service's Compose host name, use the Docker Compose environment's host address expression API from docs. Do not hardcode generated service names unless the docs or artifact prove them.
