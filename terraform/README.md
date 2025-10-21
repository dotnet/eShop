# eShop Azure Infrastructure (Terraform)

This folder contains Terraform code to provision the reference architecture for eShop on Azure, as depicted in the provided architecture diagram.

## Components Deployed
- Azure Resource Group
- Azure Virtual Network (VNET) with subnets:
  - Private Link Subnet (10.205.238.0/24)
  - APIM Subnet (10.205.239.0/24)
  - AKS Subnet (10.205.240.0/20)
- Azure DNS Zone
- Azure Kubernetes Service (AKS)
- Azure Front Door with WAF
- Azure API Management (APIM)
- Azure Private Link Service

## Usage

1. Install the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) and [Terraform](https://learn.hashicorp.com/tutorials/terraform/install-cli).
2. Authenticate with Azure:
   ```bash
   az login
   ```
3. Initialize Terraform:
   ```bash
   terraform init
   ```
4. Review and customize variables in `variables.tf` as needed.
5. Plan the deployment:
   ```bash
   terraform plan
   ```
6. Apply the deployment:
   ```bash
   terraform apply
   ```

## Notes
- This setup uses Terraform modules for VNET and AKS for best practices.
- You may need to adjust the AKS version, region, and other settings to match your requirements.
- Additional configuration may be required for production use (e.g., Front Door backend pools, APIM APIs, AKS node pools, etc.).

---

**Diagram Reference:**
![Architecture Diagram](../img/ARCHITECTURE_DIAGRAM_PLACEHOLDER.png)
