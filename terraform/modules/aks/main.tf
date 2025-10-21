module "aks" {
  source              = "Azure/aks/azurerm"
  version             = "7.4.0"
  resource_group_name = var.resource_group_name
  kubernetes_version  = var.kubernetes_version
  prefix              = var.prefix
  vnet_subnet_id      = var.vnet_subnet_id
  network_plugin      = var.network_plugin
  node_pools          = var.node_pools
}
