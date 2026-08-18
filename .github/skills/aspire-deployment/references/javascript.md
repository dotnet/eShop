# JavaScript app deployment

Use this reference whenever an Aspire deployment includes JavaScript app resources, such as Vite, React, Vue, Angular, Astro, Next.js, Nuxt, or plain Node.js apps.

JavaScript resources are not just another deployment target. They need an explicit production serving model before the target reference can be trusted. A Vite dev server that works during `aspire run` is often build-only during publish/deploy unless the AppHost says who serves the built files.

## Integration to add

```bash
aspire add javascript
```

This adds `Aspire.Hosting.JavaScript`, which provides `AddJavaScriptApp`, `AddNodeApp`, `AddViteApp`, `AddNextJsApp`, package-manager helpers, and JavaScript publish modes.

## Docs to load

Always start with current docs:

```bash
aspire docs search "javascript deployment"
aspire docs get "deploy-javascript-apps"
aspire docs get "set-up-javascript-apps-in-the-apphost"
```

Use API docs before editing. Search in the AppHost language you detected:

```bash
aspire docs api search "AddJavaScriptApp" --language csharp
aspire docs api search "AddJavaScriptApp" --language typescript
aspire docs api search "AddNodeApp" --language csharp
aspire docs api search "AddNodeApp" --language typescript
aspire docs api search "AddViteApp" --language csharp
aspire docs api search "AddViteApp" --language typescript
aspire docs api search "AddNextJsApp" --language csharp
aspire docs api search "AddNextJsApp" --language typescript
aspire docs api search "PublishAsStaticWebsite" --language csharp
aspire docs api search "PublishAsStaticWebsite" --language typescript
aspire docs api search "PublishAsNodeServer" --language csharp
aspire docs api search "PublishAsNodeServer" --language typescript
aspire docs api search "PublishAsPackageScript" --language csharp
aspire docs api search "PublishAsPackageScript" --language typescript
aspire docs api search "PublishWithContainerFiles" --language csharp
aspire docs api search "PublishWithContainerFiles" --language typescript
aspire docs api search "PublishWithStaticFiles" --language csharp
aspire docs api search "PublishWithStaticFiles" --language typescript
```

## Setup

Use `Aspire.Hosting.JavaScript` for Aspire 13+ apps. `Aspire.Hosting.NodeJs` is the old package name; do not add it for new guidance unless the target repo is intentionally pinned to older Aspire.

## Resource choice

Choose the AppHost resource based on what the JavaScript app is during local development:

| Resource | Use when | C# shape | TypeScript shape |
|----------|----------|----------|------------------|
| JavaScript app | Generic package-script-driven app | `AddJavaScriptApp(...)` | `addJavaScriptApp(...)` |
| Node app | A Node process starts a specific script file | `AddNodeApp(...)` | `addNodeApp(...)` |
| Vite app | Vite-based browser app or framework dev server | `AddViteApp(...)` | `addViteApp(...)` |
| Next.js app | Next.js app using the dedicated helper | `AddNextJsApp(...)` | `addNextJsApp(...)` |

`AddNextJsApp` and JavaScript publish methods are experimental. In C# AppHosts, follow docs for the `ASPIREJAVASCRIPT001` suppression instead of suppressing warnings broadly.

## Production serving model

Choose the production entrypoint before deploying:

| Production shape | Use when | AppHost pattern |
|------------------|----------|-----------------|
| Static frontend served by a backend | The backend should serve built frontend files from `wwwroot`, `static`, or similar | Backend resource uses `PublishWithContainerFiles(frontend, "<path>")` |
| Static frontend served by a gateway/BFF | YARP or another proxy is the public entrypoint and serves the built frontend | Gateway uses `PublishWithStaticFiles(frontend)` |
| Static frontend served by the JavaScript resource | The frontend should deploy as its own static website/container | JavaScript resource uses `PublishAsStaticWebsite(...)` |
| SSR or Node.js server with built output | The framework emits a server entrypoint/artifact | JavaScript resource uses `PublishAsNodeServer(...)` |
| SSR or Node.js server started by package script | Runtime needs package manager scripts and runtime dependencies | JavaScript resource uses `PublishAsPackageScript(...)` |
| Next.js standalone app | Next.js is configured for standalone output | Use `AddNextJsApp(...)` |

Do not assume the Vite dev server is the production server. During publish/deploy, Vite resources usually build static files unless the AppHost selects a JavaScript publish mode.

## Code changes to make

1. Run `aspire add javascript` if the AppHost does not already reference the JavaScript hosting integration.
2. Add the appropriate JavaScript resource.

   C# AppHost examples:

   ```csharp
   var frontend = builder.AddViteApp("frontend", "./frontend");
   var api = builder.AddNodeApp("api", "./api", "server.js")
       .WithHttpEndpoint(env: "PORT");
   ```

   TypeScript AppHost examples:

   ```typescript
   const frontend = await builder.addViteApp("frontend", "./frontend");
   const api = await builder
     .addNodeApp("api", "./api", "server.js")
     .withHttpEndpoint({ env: "PORT" });
   ```

3. Configure package manager and scripts only when defaults are wrong:
   - C#: `WithNpm(...)`, `WithYarn(...)`, `WithPnpm(...)`, `WithBun(...)`, `WithBuildScript(...)`, `WithRunScript(...)`.
   - TypeScript: `withNpm(...)`, `withYarn(...)`, `withPnpm(...)`, `withBun(...)`, `withBuildScript(...)`, `withRunScript(...)`.
4. Add a production serving model from the table above. This is required for build-only browser apps.
5. Add `WithExternalHttpEndpoints()` / `withExternalHttpEndpoints()` only to the resource that owns the public production HTTP surface, such as the backend, gateway, static website, Node server, or Next.js app.
6. Keep dev-only gateway/proxy wiring behind run-mode checks when it depends on a dev server URL. Production routing must be configured on the production-serving resource, not assumed from Vite dev proxy settings.
7. After choosing the production-serving resource, apply the target reference for Docker Compose, Kubernetes, Azure, or AWS.

## Target-specific notes

- Docker Compose: JavaScript resources can publish as containers or static-file-carrying build resources. Inspect generated Dockerfiles, `docker-compose.yaml`, and `.env` placeholders.
- Kubernetes and Azure Kubernetes Service (AKS): make sure the production-serving resource becomes the workload with the service/ingress/gateway exposure. Build-only resources must be consumed by another deployable resource or converted with a JavaScript publish method.
- Azure Container Apps: only the production-serving compute resource should be external. For static browser apps, `PublishAsStaticWebsite(...)` can be the external resource; for backend-served or gateway-served apps, the backend/gateway is the external resource.
- Azure App Service: use this only when the JavaScript app fits the public website model and single target-port constraints. SSR/Node apps need the right startup script/output and external HTTP endpoint.
- AWS: verify support in the AWS integrations repo. Do not assume the AWS preview deployment supports every JavaScript publish mode.

## Common pitfalls

- A Vite resource that works locally can fail deployment validation if it is still build-only and no deployed resource consumes its files.
- Vite dev-server proxy configuration does not automatically become production routing.
- `PublishWithContainerFiles(...)` copies built files into a destination container; the destination app still needs to serve those files.
- `PublishWithStaticFiles(...)` is for gateway/BFF resources such as YARP serving frontend files.
- `PublishAsPackageScript(...)` keeps runtime package dependencies and is larger than a built Node server output; prefer `PublishAsNodeServer(...)` when the framework emits a runnable server artifact.
- Next.js standalone deployment requires Next.js standalone output configuration. Validate this before deploying.
- Do not expose both a dev frontend resource and the production-serving gateway/backend unless the user intentionally wants two public surfaces.
