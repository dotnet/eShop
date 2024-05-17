# ARM Deployments

This template folder contains a suite of templates deployed through Azure Resource Manager (ARM). ARM templates are currently in GA for Azure Deployment Environments, and available to deploy for all customers.

### What will be deployed?

You can deploy the following test templates to ADE:
- [Sandbox](./Sandbox/README.md) Will deploy an empty environment, which implies generating a resource group for your environment, but with no resources.
- [WebApp](./WebApp/README.md) Will deploy an environment to host a Web App.
- [AppServiceWithCosmos - AZD](./App-Service-with-Cosmos_AZD-template/README.md) Will deploy an environment to host a Web App with a connected CosmosDB database, and is a definition compatible with AZD + ADE Public Preview
- [ContainerAppWithCosmos - AZD](./Container-App-with-Cosmos_AZD-template/README.md) Will deploy an environment to host an Azure Container App with a connected CosmosDB database, and is a definition compatible with AZD + ADE Public Preview
- [FunctionAppWithCosmos - AZD](./Function-App-with-Cosmos_AZD-template/README.md) Will deploy an environment to host an Azure Function App with a connected CosmosDB database, and is a definition compatible with AZD + ADE Public Preview 

### Learn more about ARM

- 📘 [ARM Template Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/)
- 📘 [ARM Template Best Practices](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/best-practices)
- 📘 [Understand Structure and Syntax of ARM Templates](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/syntax)
