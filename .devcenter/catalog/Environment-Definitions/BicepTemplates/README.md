# Bicep Deployments

This template folder contains a suite of templates deployed through Bicep. Deploying Bicep templates is currently a public-preview feature for Azure Deployment Environments.

### What will be deployed?

You can deploy the following test templates to ADE:
- [AppConfig](./AppConfig/appconfig.bicep) Will deploy infrastructure to create an App Configuration
- [ContainerApp](./ContainerApp/main.bicep) Will deploy a Log Analytics Workspace, a Container App Environment, and a Container App from a specified image

### Learn more about Bicep

- 📘 [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- 📘 [Bicep File Structure and Syntax](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/file)