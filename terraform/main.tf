# Azure infrastructure for eShop reference architecture

provider "azurerm" {
  features {}
}

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.0.0"
    }
  }
  required_version = ">= 1.3.0"
}

module "resource_group" {
  source   = "./modules/resource_group"
  name     = "eshop-rg"
  location = var.location
}

module "network" {
  source              = "./modules/network"
  resource_group_name = module.resource_group.name
  vnet_name           = "eshop-spoke-vnet"
  address_space       = ["10.205.0.0/16"]
  subnet_prefixes     = [
    "10.205.238.0/24", # Private Link Subnet
    "10.205.239.0/24", # APIM Subnet
    "10.205.240.0/20"  # AKS Subnet
  ]
  subnet_names        = [
    "private-link-subnet",
    "apim-subnet",
    "aks-subnet"
  ]
}

module "dns_zone" {
  source              = "./modules/dns_zone"
  name                = var.dns_zone_name
  resource_group_name = module.resource_group.name
}

module "aks" {
  source              = "./modules/aks"
  resource_group_name = module.resource_group.name
  kubernetes_version  = var.kubernetes_version
  prefix              = "eshop-aks"
  vnet_subnet_id      = module.network.vnet_subnets[2]
  network_plugin      = "azure"
  node_pools = [
    {
      name       = "default"
      vm_size    = "Standard_DS2_v2"
      node_count = 2
    }
  ]
}

module "frontdoor" {
  source              = "./modules/frontdoor"
  name                = "eshop-frontdoor"
  resource_group_name = module.resource_group.name
  sku_name            = "Premium_AzureFrontDoor"
}

module "waf" {
  source              = "./modules/waf"
  name                = "eshop-waf-policy"
  resource_group_name = module.resource_group.name
  sku_name            = "Premium_AzureFrontDoor"
}

module "apim" {
  source                = "./modules/apim"
  name                  = "eshop-apim"
  location              = module.resource_group.location
  resource_group_name   = module.resource_group.name
  publisher_name        = "eshop"
  publisher_email       = var.apim_publisher_email
  sku_name              = "Developer_1"
  virtual_network_type  = "External"
  subnet_id             = module.network.vnet_subnets[1]
}

module "private_link" {
  source      = "./modules/private_link"
  name        = "eshop-private-link"
  location    = module.resource_group.location
  resource_group_name = module.resource_group.name
  subnet_id   = module.network.vnet_subnets[0]
  load_balancer_frontend_ip_configuration_ids = []
}
