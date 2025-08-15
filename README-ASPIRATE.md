# eShop Aspirate Deployment Guide

This guide provides scripts and instructions for deploying the eShop application to Kubernetes using Aspirate, with the Identity service disabled for simplified authentication.

## Prerequisites

- Docker Desktop with Kubernetes enabled
- .NET 9 SDK
- Aspirate CLI tool installed
- kubectl configured for docker-desktop context

## Configuration

The Identity service has been disabled in the AppHost configuration (`src/eShop.AppHost/Program.cs`). All services are configured with `DisableAuth: true` to use mock authentication.

## Deployment Scripts

### 1. Regenerate and Deploy

**Description**: Completely regenerates container images and Kubernetes manifests, then deploys to the cluster.

```bash
cd src/eShop.AppHost
aspirate generate --non-interactive --secret-password "defaultpassword" --include-dashboard --image-pull-policy "Always"
aspirate apply --non-interactive --secret-password "defaultpassword" --kube-context "docker-desktop"
```

**PowerShell equivalent**:
```powershell
Set-Location src\eShop.AppHost
aspirate generate --non-interactive --secret-password "defaultpassword" --include-dashboard --image-pull-policy "Always"
aspirate apply --non-interactive --secret-password "defaultpassword" --kube-context "docker-desktop"
```

### 2. Deploy Only (using existing manifests)

**Description**: Deploys existing manifests without regenerating containers.

```bash
cd src/eShop.AppHost
aspirate apply --non-interactive --secret-password "defaultpassword" --kube-context "docker-desktop"
```

### 3. Destroy Deployment

**Description**: Removes all deployed resources from the cluster.

```bash
cd src/eShop.AppHost
aspirate destroy --non-interactive --kube-context "docker-desktop"
```

**Alternative using kubectl**:
```bash
kubectl delete -k src/eShop.AppHost/aspirate-output
```

### 4. Complete Clean Deployment (RECOMMENDED)

**Description**: **IMPORTANT - Use this for reliable deployments!** Completely removes all aspirate state, persistent volumes, and regenerates everything from scratch. This ensures consistent PostgreSQL authentication and prevents database password conflicts.

```bash
cd src/eShop.AppHost

# Destroy existing deployment
aspirate destroy --non-interactive --kube-context "docker-desktop"

# Remove persistent volumes to avoid password conflicts
kubectl delete pvc --all

# Clean local state
rm -rf aspirate-output
rm -f aspirate-state.json

# Regenerate and deploy
aspirate generate --non-interactive --secret-password "defaultpassword" --include-dashboard --image-pull-policy "Always"
aspirate apply --non-interactive --secret-password "defaultpassword" --kube-context "docker-desktop"
```

**PowerShell equivalent**:
```powershell
Set-Location src\eShop.AppHost

# Destroy existing deployment
aspirate destroy --non-interactive --kube-context "docker-desktop"

# Remove persistent volumes to avoid password conflicts
kubectl delete pvc --all

# Clean local state
Remove-Item -Recurse -Force aspirate-output -ErrorAction SilentlyContinue
Remove-Item -Force aspirate-state.json -ErrorAction SilentlyContinue

# Regenerate and deploy
aspirate generate --non-interactive --secret-password "defaultpassword" --include-dashboard --image-pull-policy "Always"
aspirate apply --non-interactive --secret-password "defaultpassword" --kube-context "docker-desktop"
```

### 5. Quick Clean Regeneration (legacy)

**Description**: Legacy method - removes aspirate state only. Use "Complete Clean Deployment" instead for better reliability.

```bash
cd src/eShop.AppHost
rm -rf aspirate-output
rm -f aspirate-state.json
aspirate generate --non-interactive --secret-password "defaultpassword" --include-dashboard --image-pull-policy "Always"
aspirate apply --non-interactive --secret-password "defaultpassword" --kube-context "docker-desktop"
```

## Verification Commands

### Check Deployment Status
```bash
kubectl get all -n default
```

### Check Pod Logs
```bash
kubectl logs -f deployment/webapp
kubectl logs -f deployment/catalog-api
kubectl logs -f deployment/basket-api
```

### Access Applications
- **Web Application**: http://localhost:30509
- **Aspire Dashboard**: http://localhost:31361

### Test API Endpoints
```bash
# Catalog API
curl http://localhost:30509/api/catalog/items

# Health Checks
curl http://localhost:30509/health
```

## Service Architecture (without Identity)

The deployment includes the following services:

- **webapp**: Main web application (accessible on port 30509)
- **catalog-api**: Product catalog service
- **basket-api**: Shopping basket service  
- **ordering-api**: Order management service
- **webhooks-api**: Webhook management service
- **webhooksclient**: Webhook client application
- **mobile-bff**: Backend for frontend (mobile)
- **order-processor**: Background order processing
- **payment-processor**: Payment processing service
- **postgres**: PostgreSQL database
- **redis**: Redis cache
- **eventbus**: RabbitMQ message bus
- **aspire-dashboard**: Monitoring dashboard

## Troubleshooting

### Common Issues

1. **PostgreSQL authentication failures**: This is the most common issue. Use the "Complete Clean Deployment" method (section 4) to resolve password conflicts between PostgreSQL container and persistent volumes.

2. **Pods in Error state**: Usually database connectivity issues. Wait for postgres pod to be ready.
   ```bash
   kubectl get pods | grep postgres
   kubectl logs postgres-0
   ```

3. **Port conflicts**: Ensure ports 30509 and 31361 are available.

4. **Image pull failures**: Ensure Docker Desktop is running and has sufficient resources.

### Reset Everything (if you encounter authentication issues)
```bash
# Use the Complete Clean Deployment method
cd src/eShop.AppHost

# Destroy everything
aspirate destroy --non-interactive --kube-context "docker-desktop"

# CRITICAL: Remove persistent volumes to fix password issues
kubectl delete pvc --all

# Clean local state
rm -rf aspirate-output aspirate-state.json

# Clean Docker images (optional)
docker image prune -f

# Restart from scratch
aspirate generate --non-interactive --secret-password "defaultpassword" --include-dashboard --image-pull-policy "Always"
aspirate apply --non-interactive --secret-password "defaultpassword" --kube-context "docker-desktop"
```

## Configuration Details

### Authentication
- Identity service is **disabled**
- All services use mock authentication
- Default user: "Demo User"
- No login required

### Database
- PostgreSQL runs as a StatefulSet with persistent storage
- Includes pgvector extension for AI features
- Databases: catalogdb, orderingdb, webhooksdb

### Networking
- All services communicate within the cluster
- External access via NodePort services
- No TLS/HTTPS in development setup

## Performance Considerations

- Resource limits are set for production-like behavior
- Persistent volumes for database storage
- Health checks configured for reliable deployments
- Rolling updates supported for zero-downtime deployments