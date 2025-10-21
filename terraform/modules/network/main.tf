module "vnet" {
  source              = "Azure/vnet/azurerm"
  version             = "4.0.0"
  resource_group_name = var.resource_group_name
  vnet_name           = var.vnet_name
  address_space       = var.address_space
  subnet_prefixes     = var.subnet_prefixes
  subnet_names        = var.subnet_names
}
