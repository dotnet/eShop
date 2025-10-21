resource "azurerm_private_link_service" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.subnet_id
  load_balancer_frontend_ip_configuration_ids = var.load_balancer_frontend_ip_configuration_ids
}
